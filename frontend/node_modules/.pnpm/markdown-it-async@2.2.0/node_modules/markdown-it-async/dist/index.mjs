import MarkdownIt from 'markdown-it';

const placeholder = (id, code) => `<pre><!--::markdown-it-async::${id}::--><code>${code}</code></pre>`;
const placeholderRe = /<pre><!--::markdown-it-async::(\w+)::--><code>[\s\S]*?<\/code><\/pre>/g;
function randStr() {
  return Math.random().toString(36).slice(2) + Math.random().toString(36).slice(2);
}
class MarkdownItAsync extends MarkdownIt {
  placeholderMap;
  disableWarn = false;
  constructor(...args) {
    const map = /* @__PURE__ */ new Map();
    const options = args.length === 2 ? args[1] : args[0];
    if (options && "highlight" in options)
      options.highlight = wrapHightlight(options.highlight, map);
    super(...args);
    this.placeholderMap = map;
  }
  // implementation
  use(plugin, ...params) {
    return super.use(plugin, ...params);
  }
  render(src, env) {
    if (this.options.warnOnSyncRender && !this.disableWarn) {
      console.warn("[markdown-it-async] Please use `md.renderAsync` instead of `md.render`");
    }
    return super.render(src, env);
  }
  async renderAsync(src, env) {
    this.options.highlight = wrapHightlight(this.options.highlight, this.placeholderMap);
    this.disableWarn = true;
    const result = this.render(src, env);
    this.disableWarn = false;
    return replaceAsync(result, placeholderRe, async (match, id) => {
      if (!this.placeholderMap.has(id))
        throw new Error(`Unknown highlight placeholder id: ${id}`);
      const [promise, _str, lang, _attrs] = this.placeholderMap.get(id);
      const result2 = await promise || "";
      this.placeholderMap.delete(id);
      if (result2.startsWith("<pre"))
        return result2;
      else
        return `<pre><code class="language-${lang}">${result2}</code></pre>`;
    });
  }
}
function createMarkdownItAsync(...args) {
  return new MarkdownItAsync(...args);
}
function replaceAsync(string, searchValue, replacer) {
  try {
    if (typeof replacer === "function") {
      const values = [];
      String.prototype.replace.call(string, searchValue, (...args) => {
        values.push(replacer(...args));
        return "";
      });
      return Promise.all(values).then((resolvedValues) => {
        return String.prototype.replace.call(string, searchValue, () => {
          return resolvedValues.shift() || "";
        });
      });
    } else {
      return Promise.resolve(
        String.prototype.replace.call(string, searchValue, replacer)
      );
    }
  } catch (error) {
    return Promise.reject(error);
  }
}
const wrappedSet = /* @__PURE__ */ new WeakSet();
function escapeHtml(unsafe) {
  return unsafe.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
}
function wrapHightlight(highlight, map) {
  if (!highlight)
    return void 0;
  if (wrappedSet.has(highlight))
    return highlight;
  const wrapped = (str, lang, attrs) => {
    const promise = highlight(str, lang, attrs);
    if (typeof promise === "string")
      return promise;
    const id = randStr();
    map.set(id, [promise, str, lang, attrs]);
    let code = str;
    if (code.endsWith("\n"))
      code = code.slice(0, -1);
    code = escapeHtml(code);
    return placeholder(id, code);
  };
  wrappedSet.add(wrapped);
  return wrapped;
}

export { MarkdownItAsync, createMarkdownItAsync, createMarkdownItAsync as default, replaceAsync };
