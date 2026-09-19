window.addEventListener('RAWebReady', ({ detail: raweb }) => {
  const WelcomePage = createWelcomePageComponent(raweb);
  raweb.router.addRoute({ path: '/demo', name: 'demo', component: WelcomePage });

  // re-evaluate the current route in case we're already on /demo on first load
  raweb.router.replace(raweb.router.currentRoute.value.fullPath);
});

window.addEventListener('RAWebAppMounted', ({ detail: raweb }) => {
  const navList = document.querySelector('.nav-rail > nav > ul');
  const favoritesItem = navList?.querySelector("li[data-id='favorites']");
  if (!navList) return;

  const container = document.createElement('li');
  const Component = createWelcomeNavRailLinkComponent(raweb);
  const subApp = raweb.vue.createApp(Component);
  subApp.use(raweb.router);
  subApp.mount(container);

  if (favoritesItem) {
    favoritesItem.before(container);
  } else {
    navList.prepend(container);
  }

  // update the position of the selected nav rail item indicator
  const activeButton = document.querySelector('.nav-rail .button.active');
  if (activeButton instanceof HTMLElement) {
    raweb.stores.useNavigationRailStore().trackHandle?.select(activeButton, 24);
  }
});

/** @param {RAWebReadyEventData} raweb */
function createWelcomePageComponent(raweb) {
  const { Button, TextBlock } = raweb.components;
  const { h } = raweb.vue;
  const { useCoreDataStore } = raweb.stores;
  const { RouterLink } = raweb.components;

  return {
    setup() {
      const { docsUrl } = useCoreDataStore();
      return { docsUrl };
    },
    render() {
      return h('div', { class: 'welcome-page' }, [
        h('div', { class: 'titlebar-row' }, [h(TextBlock, { variant: 'title', tag: 'h1' }, () => 'Demo')]),
        h('div', { class: 'hero' }, [
          h(TextBlock, { variant: 'subtitle' }, () => 'Welcome to the RemoteApps & Devices demo'),
          h('div', { class: 'prose' }, [
            h(
              TextBlock,
              { block: true },
              () =>
                'RemoteApps & Devices is the web interface for RAWeb, a RemoteApp and Desktop workspace provider for Windows 10, 11, and Server.'
            ),
            h(TextBlock, { block: true }, () => [
              'This demo is a chance to explore the web interface and management capabilities before installing RAWeb yourself. Look around the Apps and Devices pages to see example RemoteApps and desktops. Check out the Settings pages to see the available options and management features. Learn more on the ',
              // @ts-expect-error
              h('a', { href: this.docsUrl, target: '_blank', class: 'inline-link' }, 'wiki'),
              '.',
            ]),
          ]),
          h('div', { class: 'button-row' }, [
            // @ts-expect-error
            h(RouterLink, { to: '/devices', custom: true }, ({ href, navigate }) =>
              h(Button, { href, variant: 'accent', onClick: navigate }, () => 'Go to devices')
            ),
            // @ts-expect-error\
            h(RouterLink, { to: '/apps', custom: true }, ({ href, navigate }) =>
              h(Button, { href, variant: 'accent', onClick: navigate }, () => 'Go to apps')
            ),
          ]),
          h('div', { class: 'button-row' }, [
            h(
              Button,
              {
                variant: 'hyperlink',
                href: 'https://github.com/kimmknight/raweb',
                target: '_blank',
                style: 'margin-top: -1rem;',
              },
              () => 'View project on GitHub'
            ),
          ]),
        ]),
      ]);
    },
  };
}

/** @param {RAWebReadyEventData} raweb */
function createWelcomeNavRailLinkComponent(raweb) {
  const { AnimatedNavigationItemIndicator, RailButton, RouterLink } = raweb.components;
  const { h } = raweb.vue;
  const { useNavigationRailStore } = raweb.stores;

  const megaphoneOutline = h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 24 24' }, [
    h('path', {
      d: 'M21.907 5.622c.062.208.093.424.093.641V17.74a2.25 2.25 0 0 1-2.891 2.156l-5.514-1.64a4.002 4.002 0 0 1-7.59-1.556L6 16.5l-.001-.5-2.39-.711A2.25 2.25 0 0 1 2 13.131V10.87a2.25 2.25 0 0 1 1.61-2.156l15.5-4.606a2.25 2.25 0 0 1 2.797 1.515ZM7.499 16.445l.001.054a2.5 2.5 0 0 0 4.624 1.321l-4.625-1.375Zm12.037-10.9-15.5 4.605a.75.75 0 0 0-.536.72v2.261a.75.75 0 0 0 .536.72l15.5 4.607a.75.75 0 0 0 .964-.72V6.264a.75.75 0 0 0-.964-.719Z',
      fill: 'currentColor',
    }),
  ]);
  const megaphoneFilled = h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 24 24' }, [
    h('path', {
      d: 'M21.907 5.622c.062.208.093.424.093.641V17.74a2.25 2.25 0 0 1-2.891 2.156l-5.514-1.64a4.002 4.002 0 0 1-7.59-1.556L6 16.5l-.001-.5-2.39-.711A2.25 2.25 0 0 1 2 13.131V10.87a2.25 2.25 0 0 1 1.61-2.156l15.5-4.606a2.25 2.25 0 0 1 2.797 1.515ZM7.499 16.445l.001.054a2.5 2.5 0 0 0 4.624 1.321l-4.625-1.375Z',
      fill: 'currentColor',
    }),
  ]);

  return {
    render() {
      // @ts-expect-error
      return h(RouterLink, { to: '/demo', custom: true }, ({ href, isActive, navigate }) =>
        h(
          AnimatedNavigationItemIndicator.Selectable,
          // @ts-expect-error
          { selected: isActive, indicatorSize: 24, trackHandle: useNavigationRailStore().trackHandle },
          () =>
            h(
              RailButton,
              { href, active: isActive, onClick: navigate },
              {
                icon: () => megaphoneOutline,
                'icon-active': () => megaphoneFilled,
                default: () => 'Demo info',
              }
            )
        )
      );
    },
  };
}
