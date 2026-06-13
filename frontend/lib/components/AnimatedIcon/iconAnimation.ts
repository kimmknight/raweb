import type { InjectionKey } from 'vue';

export interface IconAnimationHandle {
  press(): void;
  release(): void | Promise<void>;
}

export const registerIconAnimationKey: InjectionKey<(handle: IconAnimationHandle | undefined) => void> =
  Symbol('registerIconAnimation');
