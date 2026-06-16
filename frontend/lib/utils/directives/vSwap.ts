import { Directive } from 'vue';

type SwapDirective = Directive<HTMLElement, string>;

declare module 'vue' {
  interface GlobalDirectives {
    vSwap: SwapDirective;
  }
}

export const vSwap: SwapDirective = (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
};
