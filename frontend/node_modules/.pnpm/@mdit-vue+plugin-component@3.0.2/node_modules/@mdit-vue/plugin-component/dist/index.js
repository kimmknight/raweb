//#region src/html-re.ts
const attr_name = "[a-zA-Z_:@][a-zA-Z0-9:._-]*";
const unquoted = "[^\"'=<>`\\x00-\\x20]+";
const attr_value = "(?:" + unquoted + "|'[^']*'|\"[^\"]*\")";
const attribute = "(?:\\s+" + attr_name + "(?:\\s*=\\s*(?:[^\"'=<>`\\x00-\\x20]+|'[^']*'|\"[^\"]*\"))?)";
const open_tag = "<[A-Za-z][A-Za-z0-9\\-]*" + attribute + "*\\s*\\/?>";
const HTML_TAG_RE = /* @__PURE__ */ new RegExp("^(?:" + open_tag + "|<\\/[A-Za-z][A-Za-z0-9\\-]*\\s*>|<!---->|<!--(?:-?[^>-])(?:-?[^-])*-->|<[?][\\s\\S]*?[?]>|<![A-Z]+\\s+[^>]*>|<!\\[CDATA\\[[\\s\\S]*?\\]\\]>)");
const HTML_OPEN_CLOSE_TAG_RE = /* @__PURE__ */ new RegExp("^(?:" + open_tag + "|<\\/[A-Za-z][A-Za-z0-9\\-]*\\s*>)");
const HTML_SELF_CLOSING_TAG_RE = /* @__PURE__ */ new RegExp("^<[A-Za-z][A-Za-z0-9\\-]*" + attribute + "*\\s*\\/>");
const HTML_OPEN_AND_CLOSE_TAG_IN_THE_SAME_LINE_RE = /* @__PURE__ */ new RegExp("^<([A-Za-z][A-Za-z0-9\\-]*)" + attribute + "*\\s*>.*<\\/\\1\\s*>");

//#endregion
//#region src/tags.ts
/**
* List of block tags
*
* @see https://spec.commonmark.org/0.30/#html-blocks
* @see https://github.com/markdown-it/markdown-it/blob/master/lib/common/html_blocks.mjs
*/
const TAGS_BLOCK = [
	"address",
	"article",
	"aside",
	"base",
	"basefont",
	"blockquote",
	"body",
	"caption",
	"center",
	"col",
	"colgroup",
	"dd",
	"details",
	"dialog",
	"dir",
	"div",
	"dl",
	"dt",
	"fieldset",
	"figcaption",
	"figure",
	"footer",
	"form",
	"frame",
	"frameset",
	"h1",
	"h2",
	"h3",
	"h4",
	"h5",
	"h6",
	"head",
	"header",
	"hr",
	"html",
	"iframe",
	"legend",
	"li",
	"link",
	"main",
	"menu",
	"menuitem",
	"nav",
	"noframes",
	"ol",
	"optgroup",
	"option",
	"p",
	"param",
	"search",
	"section",
	"summary",
	"table",
	"tbody",
	"td",
	"tfoot",
	"th",
	"thead",
	"title",
	"tr",
	"track",
	"ul"
];
/**
* According to markdown spec, all non-block html tags are treated as "inline"
* tags (wrapped with <p></p>), including those "unknown" tags
*
* Therefore, markdown-it processes "inline" tags and "unknown" tags in the same
* way, and does not care if a tag is "inline" or "unknown"
*
* As we want to take those "unknown" tags as custom components, we should
* treat them as "block" tags
*
* So we have to distinguish between "inline" and "unknown" tags ourselves
*
* The inline tags list comes from MDN
*
* @see https://spec.commonmark.org/0.30/#raw-html
* @see https://developer.mozilla.org/en-US/docs/Web/HTML/Inline_elements
*/
const TAGS_INLINE = [
	"a",
	"abbr",
	"acronym",
	"audio",
	"b",
	"bdi",
	"bdo",
	"big",
	"br",
	"button",
	"canvas",
	"cite",
	"code",
	"data",
	"datalist",
	"del",
	"dfn",
	"em",
	"embed",
	"i",
	"iframe",
	"img",
	"input",
	"ins",
	"kbd",
	"label",
	"map",
	"mark",
	"meter",
	"noscript",
	"object",
	"output",
	"picture",
	"progress",
	"q",
	"ruby",
	"s",
	"samp",
	"script",
	"select",
	"slot",
	"small",
	"span",
	"strong",
	"sub",
	"sup",
	"svg",
	"template",
	"textarea",
	"time",
	"u",
	"tt",
	"var",
	"video",
	"wbr"
];
/**
* Tags of Vue built-in components
*
* @see https://vuejs.org/api/built-in-components.html
* @see https://vuejs.org/api/built-in-special-elements.html
*/
const TAGS_VUE_RESERVED = [
	"template",
	"component",
	"transition",
	"transition-group",
	"keep-alive",
	"slot",
	"teleport"
];

//#endregion
//#region src/html-block-rule.ts
/**
* ADDED: wrap the `HTML_SEQUENCES` with a function, because we allow user options
* to customize the block tags and inline tags
*/
const createHtmlSequences = ({ blockTags, inlineTags }) => {
	const forceBlockTags = [...blockTags, ...TAGS_BLOCK];
	const forceInlineTags = [...inlineTags, ...TAGS_INLINE.filter((item) => !TAGS_VUE_RESERVED.includes(item))];
	const HTML_SEQUENCES = [
		[
			/^<(script|pre|style)(?=(\s|>|$))/i,
			/<\/(script|pre|style)>/i,
			true
		],
		[
			/^<!--/,
			/-->/,
			true
		],
		[
			/^<\?/,
			/\?>/,
			true
		],
		[
			/^<![A-Z]/,
			/>/,
			true
		],
		[
			/^<!\[CDATA\[/,
			/\]\]>/,
			true
		],
		[
			new RegExp("^</?(" + forceBlockTags.join("|") + ")(?=(\\s|/?>|$))", "i"),
			/^$/,
			true
		],
		[
			/* @__PURE__ */ new RegExp("^</?(?!(" + forceInlineTags.join("|") + ")(?![\\w-]))[A-Za-z][A-Za-z0-9\\-]*(?=(\\s|/?>|$))"),
			/^$/,
			true
		],
		[
			/* @__PURE__ */ new RegExp(HTML_OPEN_CLOSE_TAG_RE.source + "\\s*$"),
			/^$/,
			false
		]
	];
	return HTML_SEQUENCES;
};
const createHtmlBlockRule = (options) => {
	const HTML_SEQUENCES = createHtmlSequences(options);
	return (state, startLine, endLine, silent) => {
		let i;
		let nextLine;
		let lineText;
		let pos = state.bMarks[startLine] + state.tShift[startLine];
		let max = state.eMarks[startLine];
		if (state.sCount[startLine] - state.blkIndent >= 4) return false;
		if (!state.md.options.html) return false;
		if (state.src.charCodeAt(pos) !== 60) return false;
		lineText = state.src.slice(pos, max);
		for (i = 0; i < HTML_SEQUENCES.length; i++) if (HTML_SEQUENCES[i][0].test(lineText)) break;
		if (i === HTML_SEQUENCES.length) return false;
		if (silent) return HTML_SEQUENCES[i][2];
		if (i === 6) {
			const match = lineText.match(HTML_SELF_CLOSING_TAG_RE) ?? lineText.match(HTML_OPEN_AND_CLOSE_TAG_IN_THE_SAME_LINE_RE);
			if (match) {
				state.line = startLine + 1;
				let token$1 = state.push("html_inline", "", 0);
				token$1.content = match[0];
				token$1.map = [startLine, state.line];
				token$1 = state.push("inline", "", 0);
				token$1.content = lineText.slice(match[0].length);
				token$1.map = [startLine, state.line];
				token$1.children = [];
				return true;
			}
		}
		nextLine = startLine + 1;
		if (!HTML_SEQUENCES[i][1].test(lineText)) for (; nextLine < endLine; nextLine++) {
			if (state.sCount[nextLine] < state.blkIndent) break;
			pos = state.bMarks[nextLine] + state.tShift[nextLine];
			max = state.eMarks[nextLine];
			lineText = state.src.slice(pos, max);
			if (HTML_SEQUENCES[i][1].test(lineText)) {
				if (lineText.length !== 0) nextLine++;
				break;
			}
		}
		state.line = nextLine;
		const token = state.push("html_block", "", 0);
		token.map = [startLine, nextLine];
		token.content = state.getLines(startLine, nextLine, state.blkIndent, true);
		return true;
	};
};

//#endregion
//#region src/html-inline-rule.ts
const isLetter = (ch) => {
	const lc = ch | 32;
	return lc >= 97 && lc <= 122;
};
const htmlInlineRule = (state, silent) => {
	const { pos } = state;
	if (!state.md.options.html) return false;
	const max = state.posMax;
	if (state.src.charCodeAt(pos) !== 60 || pos + 2 >= max) return false;
	const ch = state.src.charCodeAt(pos + 1);
	if (ch !== 33 && ch !== 63 && ch !== 47 && !isLetter(ch)) return false;
	const match = state.src.slice(pos).match(HTML_TAG_RE);
	if (!match) return false;
	if (!silent) {
		const token = state.push("html_inline", "", 0);
		token.content = state.src.slice(pos, pos + match[0].length);
	}
	state.pos += match[0].length;
	return true;
};

//#endregion
//#region src/component-plugin.ts
/**
* Allows better use of Vue components in Markdown
*/
const componentPlugin = (md, { blockTags = [], inlineTags = [] } = {}) => {
	const htmlBlockRule = createHtmlBlockRule({
		blockTags,
		inlineTags
	});
	md.block.ruler.at("html_block", htmlBlockRule, { alt: [
		"paragraph",
		"reference",
		"blockquote"
	] });
	md.inline.ruler.at("html_inline", htmlInlineRule);
};

//#endregion
export { HTML_OPEN_AND_CLOSE_TAG_IN_THE_SAME_LINE_RE, HTML_OPEN_CLOSE_TAG_RE, HTML_SELF_CLOSING_TAG_RE, HTML_TAG_RE, TAGS_BLOCK, TAGS_INLINE, TAGS_VUE_RESERVED, componentPlugin, createHtmlBlockRule, htmlInlineRule };