<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Register Src="~/app/lib/controls/AppRoot.ascx" TagName="AppRoot" TagPrefix="raweb" %>

<raweb:AppRoot runat="server" />

<!-- The following script tags are used to load Vue and the Vue SFC loader. They are necessary for the Vue app to work. -->
<%-- When developing, switch to the development importmap for bettwer logging and error messages. --%>
<%-- Don't forget to swtich back to the production importmap before commiting your changes! --%>
<%-- Development: --%>
<%-- <script type="importmap">
    {
        "imports": {
            "vue": "<%= ResolveUrl("~/app/lib/modules/vue.dev.mjs") %>",
            "vue3-sfc-loader": "<%= ResolveUrl("~/app/lib/modules/vue3-sfc-loader.dev.mjs") %>",
            "devalue": "<%= ResolveUrl("~/app/lib/modules/devalue.min.mjs") %>",
            "path": "<%= ResolveUrl("~/app/lib/modules/path-browserify.mjs") %>"
        }
    }
</script> --%>
<%-- Production: --%>
<script type="importmap">
    {
        "imports": {
            "vue": "<%= ResolveUrl("~/app/lib/modules/vue.prod.mjs") %>",
            "vue3-sfc-loader": "<%= ResolveUrl("~/app/lib/modules/vue3-sfc-loader.min.mjs") %>",
            "devalue": "<%= ResolveUrl("~/app/lib/modules/devalue.min.mjs") %>",
            "path": "<%= ResolveUrl("~/app/lib/modules/path-browserify.mjs") %>"
        }
    }
</script>

<script type="module">
    import * as Vue from 'vue';
    import { loadModule } from 'vue3-sfc-loader';
    import { posix as Path } from 'path';

    const aliases = {
        'devalue': '<%= ResolveUrl("~/app/lib/modules/devalue.min.mjs") %>',
        '$components': '<%= ResolveUrl("~/app/lib/components/") %>'.slice(0, -1), // remove trailing slash
        '$utils': '<%= ResolveUrl("~/app/lib/utils/") %>'.slice(0, -1),
        '$icons': '<%= ResolveUrl("~/app/lib/assets/icons.ts") %>',
    }

    // configure options for Vue Single File Component (SFC) loader
    // https://github.com/FranckFreiburger/vue3-sfc-loader/tree/main
    const componentLoaderOptions = {
        moduleCache: { vue: Vue },
        getFile: (url) => fetch(url).then((response) => response.text()),
        addStyle: (styleString) => {
            const styleTag = document.createElement("style");
            styleTag.textContent = styleString;
            const ref = document.head.getElementsByTagName("style")[0] || null;
            document.head.insertBefore(styleTag, ref);
        },
        log: (type, data) => { console[type](data); },

        // adapted from the original function at
        // https://github.com/FranckFreiburger/vue3-sfc-loader/blob/bed17ac565fc36f74970db56ca1a12fe0cc412f4/src/index.ts#L65
        pathResolve: ({ refPath, relPath }, options) => {
            const { getPathname } = options;

            // resolve aliases
            Object.entries(aliases).forEach(([alias, replacement]) => {
                if (relPath.startsWith(alias)) {
                    relPath = relPath.replace(alias, replacement);
                }
            });

            // if there is no extension, assume it is a folder
            const relPathMissingExtension = !relPath.includes('.') && !relPath.endsWith('/') && relPath.startsWith('/');
            if (relPathMissingExtension) {
                relPath += '/index.mjs';
            }

            // initial resolution: refPath is not defined
            if (refPath === undefined) return relPath;

            const relPathStr = relPath.toString();

            // is non-relative path ?
            if (relPathStr[0] !== '.') return relPath;

            // note :
            //  normalize('./test') -> 'test'
            //  normalize('/test') -> '/test'
            return Path.normalize(Path.join(Path.dirname(getPathname(refPath.toString())), relPathStr));
        },
    };


    // define the component for this page
    const AppComponent = Vue.defineAsyncComponent(() => {
        const modulePath = '<%= ResolveUrl("~/app/lib/App.vue") %>';
        return loadModule(modulePath, componentLoaderOptions)
    })

    // create the Vue app in the #app div
    const app = Vue.createApp(AppComponent)
    app.directive('swap', (el, binding) => {
        if (el.parentNode) {
            el.outerHTML = binding.value;
        }
    });
    app.mount('#app');
</script>