import { Directive } from 'vue';

interface DropZoneConfig {
  handler: (count: number, files?: FileList, invalidMimeTypes?: string[], foundMimeTypes?: string[]) => void;
  mimeTypes?: string[];
}

type DropZoneDirective = Directive<HTMLElement, DropZoneConfig>;

declare module 'vue' {
  interface GlobalDirectives {
    vDropZone: DropZoneDirective;
  }
}

/**
 * Reads a FileList, and for any file that has an empty type, fills it
 * with:
 * - 'application/x-rdp' for .rdp files
 * - 'application/x-tsresource' for .tsresource and .resource files
 * - 'application/x-tsresourcebundle' for .tsresourcebundle files
 * - 'application/octet-stream' for all other files
 */
function fillEmptyMimeTypes(files?: FileList): FileList {
  if (!files) {
    return new DataTransfer().files;
  }

  const filledFiles = Array.from(files).map((file) => {
    if (file.type) {
      return file;
    }

    const extension = file.name.split('.').pop()?.toLowerCase();

    let type = 'application/octet-stream';
    if (extension === 'tsresource' || extension === 'resource') {
      type = 'application/x-tsresource';
    } else if (extension === 'tsresourcebundle') {
      type = 'application/x-tsresourcebundle';
    } else if (extension === 'rdp') {
      type = 'application/x-rdp';
    }

    return new File([file], file.name, { type });
  });

  const dt = new DataTransfer();
  filledFiles.forEach((file) => dt.items.add(file));
  return dt.files;
}

export const vDropZone: DropZoneDirective = {
  async mounted(element: HTMLElement, binding) {
    const allowedMimeTypes = binding.value?.mimeTypes;
    const areAllAllowed = (files?: FileList) => {
      if (!allowedMimeTypes || !files || files.length === 0) {
        return {
          isTrue: true,
          foundDisallowedMimeTypes: [],
          foundAllowedMimeTypes: [],
        };
      }

      const foundDisallowedMimeTypes = [
        ...new Set(
          Array.from(files)
            .filter((file) => !allowedMimeTypes.includes(file.type))
            .map((file) => file.type)
        ),
      ];
      const foundAllowedMimeTypes = [
        ...new Set(
          Array.from(files)
            .filter((file) => allowedMimeTypes.includes(file.type))
            .map((file) => file.type)
        ),
      ];
      const areAllAllowed = foundDisallowedMimeTypes.length === 0;

      return {
        isTrue: areAllAllowed,
        foundDisallowedMimeTypes,
        foundAllowedMimeTypes,
      };
    };

    function handleDragEnter(event: DragEvent) {
      event.stopPropagation();
      event.preventDefault();
    }

    function handleDragOver(event: DragEvent) {
      event.stopPropagation();
      event.preventDefault();
    }

    function handleDrop(event: DragEvent) {
      event.stopPropagation();
      event.preventDefault();

      const dataTransfer = event.dataTransfer;
      if (!dataTransfer) {
        return;
      }

      const files = fillEmptyMimeTypes(dataTransfer.files);
      const {
        isTrue: allMimeTypesAllowed,
        foundDisallowedMimeTypes,
        foundAllowedMimeTypes,
      } = areAllAllowed(files);

      if (!binding.value || typeof binding.value.handler !== 'function') {
        console.warn('Received one or more files, but no handler is available');
        return;
      }

      if (!allMimeTypesAllowed) {
        binding.value.handler(files.length, undefined, foundDisallowedMimeTypes, foundAllowedMimeTypes);
        return;
      }

      if (files && files.length > 0) {
        binding.value.handler(files.length, files, undefined, foundAllowedMimeTypes);
        return;
      }
    }

    element.addEventListener('dragenter', handleDragEnter);
    element.addEventListener('dragover', handleDragOver);
    element.addEventListener('drop', handleDrop);

    // store the handlers on the element so we can remove them later
    (element as any)._vDropZoneHandlers = { handleDragEnter, handleDragOver, handleDrop };
  },
  unmounted(element: HTMLElement) {
    const handlers = (element as any)._dropZoneHandlers;
    if (handlers) {
      element.removeEventListener('dragenter', handlers.handleDragEnter);
      element.removeEventListener('dragover', handlers.handleDragOver);
      element.removeEventListener('drop', handlers.handleDrop);
      delete (element as any)._dropZoneHandlers;
    }
  },
};
