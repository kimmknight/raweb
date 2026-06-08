using Microsoft.UI.Reactor;

ReactorApp.Run(ctx => {
  ReactorWindow? win = null;
  win = ReactorApp.OpenWindow(
    new WindowSpec {
      Title = "RAWeb",
      Width = 1200,
      Height = 800,
      ExtendsContentIntoTitleBar = true,
    }.WithPersistence("raweb-main-window"),
    () => {
      return new App(getWindow: () => win);
    }
  );
});
