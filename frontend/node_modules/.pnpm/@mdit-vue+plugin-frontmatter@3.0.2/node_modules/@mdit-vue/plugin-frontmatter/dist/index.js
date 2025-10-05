import grayMatter from "gray-matter";

//#region src/frontmatter-plugin.ts
/**
* Get markdown frontmatter and excerpt
*
* Extract them into env
*/
const frontmatterPlugin = (md, { grayMatterOptions, renderExcerpt = true } = {}) => {
	const parse = md.parse.bind(md);
	md.parse = (src, env = {}) => {
		const { data, content, excerpt = "" } = grayMatter(src, grayMatterOptions);
		env.content = content;
		env.frontmatter = {
			...env.frontmatter,
			...data
		};
		env.excerpt = renderExcerpt && excerpt ? md.render(excerpt, { ...env }) : excerpt;
		return parse(content, env);
	};
};

//#endregion
export { frontmatterPlugin };