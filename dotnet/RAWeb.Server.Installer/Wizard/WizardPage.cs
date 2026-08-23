using System.Windows.Controls;

namespace RAWeb.Server.Installer.Wizard;

public enum WizardNavigationDirection {
  Forward,
  Backward,
}

/// <summary>
/// Base for every wizard page. The shell owns navigation; a page only says whether it is ready to
/// advance, and may ask to advance itself once its work finishes.
/// </summary>
public abstract class WizardPage : UserControl {
  private bool _canGoNext = true;

  protected WizardState State { get; private set; } = null!;

  public void Attach(WizardState state) => State = state;

  public abstract string Title { get; }

  public virtual string? Description => null;

  public virtual string NextText => "Next";

  public virtual bool ShowBack { get; set; } = true;

  public virtual bool ShowCancel => true;

  /// <summary>
  /// True while the page is doing work that must not be interrupted by navigation.
  /// </summary>
  public virtual bool IsBusy => false;

  /// <summary>
  /// An optional extra footer button shown next to Next/Finish, for a page-specific action that is
  /// not navigation (for example, launching a companion app after a successful install).
  /// </summary>
  public virtual bool ShowSecondaryAction => false;

  public virtual string SecondaryActionText => "";

  public virtual void OnSecondaryAction() {
  }

  public bool CanGoNext {
    get => _canGoNext;
    protected set {
      if (_canGoNext == value) {
        return;
      }

      _canGoNext = value;
      NavigationStateChanged?.Invoke();
    }
  }

  /// <summary>
  /// Raised when <see cref="CanGoNext"/> or any other footer-affecting state changes.
  /// </summary>
  public event Action? NavigationStateChanged;

  /// <summary>
  /// Raised by pages that advance on their own once their work completes.
  /// </summary>
  public event Action? RequestNext;

  protected void RaiseNavigationStateChanged() => NavigationStateChanged?.Invoke();

  protected void RaiseRequestNext() => RequestNext?.Invoke();

  /// <summary>
  /// Called each time the page becomes visible.
  /// <br/><br/>
  /// Pages that auto-advance MUST check whether the direction is backward,
  /// and if so, must not auto-advance.
  /// </summary>
  public virtual void OnEnter(WizardNavigationDirection direction) {
  }

  /// <summary>
  /// Returns false to keep the user on this page. This is useful for when
  /// validation fails or when the page is busy doing work that must not be interrupted.
  /// </summary>
  public virtual Task<bool> OnNextAsync() => Task.FromResult(true);

  /// <summary>
  /// When true, the page will be skipped. <see cref="RequestNext"/> will be called. 
  /// </summary>
  public virtual bool ShouldSkip() => false;

  /// <summary>
  /// Called when Cancel is clicked.
  /// <br/><br/>
  /// If a page overrides this to return `true`, the wizard will not close
  /// and the page will be fully responsible for deciding what to do
  /// and when to close the wizard. This is useful for pages that must perform
  /// cleanup or confirmation before the wizard closes.
  /// </summary>
  public virtual bool OnCancelRequested() => false;
}
