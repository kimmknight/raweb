import { t } from 'i18next';

/**
 * Opens a file picker to select an image file and reads its content.
 *
 * The image is converted to JPEG format using a canvas.
 * The processed image is returned as a blob containing the JPEG.
 */
export async function pickImageFile() {
  return new Promise<Blob>((resolve, reject) => {
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.multiple = false;
    input.onchange = (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (!file) {
        return;
      }

      const reader = new FileReader();
      reader.readAsArrayBuffer(file);
      reader.onload = async (readerEvent) => {
        try {
          var arrayBuffer = readerEvent.target?.result;
          if (!arrayBuffer || !(arrayBuffer instanceof ArrayBuffer)) {
            return;
          }

          // read the ArrayBuffer as an image
          const img = new Image();
          img.src = URL.createObjectURL(new Blob([arrayBuffer]));
          await img.decode();

          // draw the image to a canvas
          const canvas = document.createElement('canvas');
          canvas.width = img.width;
          canvas.height = img.height;
          const ctx = canvas.getContext('2d');
          if (!ctx) {
            reject('Could not get canvas context');
            input.remove();
            return;
          }
          ctx.drawImage(img, 0, 0);
          URL.revokeObjectURL(img.src);

          // compress the image if it is larger than 1 MB
          const compressedBlob = await compress(canvas, 1024 * 1024);
          if (!compressedBlob) {
            reject(t('registryApps.manager.pickImageFile.imageTooLarge'));
            input.remove();
            return;
          }

          resolve(compressedBlob);
          input.remove();
        } catch (error) {
          console.error('Error processing image file:', error);
          reject('Error processing image file');
          input.remove();
        }
      };
    };

    input.oncancel = () => {
      reject('User cancelled image selection');
      input.remove();
    };

    input.click();
  });
}

/**
 * Compresses a canvas to be under the specified byte size
 * by reducing WEBP quality, then converting back to PNG.
 * Returns null if unable to compress under the size limit.
 * @param canvas
 * @param bytesSize
 * @returns
 */
async function compress(canvas: HTMLCanvasElement, byteLimit = 1024 * 1024) {
  // restrict width to max 1920px
  if (canvas.width > 1920) {
    const scale = 1920 / canvas.width;
    const resizedCanvas = document.createElement('canvas');
    resizedCanvas.width = 1920;
    resizedCanvas.height = canvas.height * scale;
    const ctx = resizedCanvas.getContext('2d');
    if (!ctx) {
      return null;
    }
    ctx.drawImage(canvas, 0, 0, resizedCanvas.width, resizedCanvas.height);
    canvas = resizedCanvas;
  }

  // restrict height to max 1200px
  if (canvas.height > 1200) {
    const scale = 1200 / canvas.height;
    const resizedCanvas = document.createElement('canvas');
    resizedCanvas.width = canvas.width * scale;
    resizedCanvas.height = 1200;
    const ctx = resizedCanvas.getContext('2d');
    if (!ctx) {
      return null;
    }
    ctx.drawImage(canvas, 0, 0, resizedCanvas.width, resizedCanvas.height);
    canvas = resizedCanvas;
  }

  // create a working canvas
  let current = canvas;

  // keep reducing size until under byte limit
  let scale = 1.0;
  while (true) {
    if (scale <= 0.1) {
      return null; // stop before it becomes too small
    }

    const blob = await new Promise<Blob | null>((resolve) => current.toBlob(resolve, 'image/png'));
    if (!blob) return null;

    if (blob.size <= byteLimit) return blob;

    // scale down and try again
    scale *= 0.9;
    const scaled = document.createElement('canvas');
    scaled.width = Math.round(canvas.width * scale);
    scaled.height = Math.round(canvas.height * scale);
    const ctx = scaled.getContext('2d');
    if (!ctx) return null;
    ctx.drawImage(canvas, 0, 0, scaled.width, scaled.height);
    current = scaled;
  }
}
