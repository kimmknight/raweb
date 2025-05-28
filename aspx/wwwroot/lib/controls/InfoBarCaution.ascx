<%@ Control Language="C#" AutoEventWireup="true" %>

<script runat="server">
    public string Title {get; set; }
    public string Message {get; set; }
    public string style {get; set; }
    public string href {get; set; }
    public string AnchorText {get; set; }
</script>

<div class="info-bar severity-caution" role="alert" style="<%= style %>">
    <div class="info-bar-icon">
        <span class="info-badge severity-caution">
            <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="406 86 213 875">
                <path
                    fill="currentColor"
                    d="M426.5,512L426.5,170.5C426.5,158.833 428.75,147.833 433.25,137.5C437.75,127.167 443.917,118.167 451.75,110.5C459.583,102.833 468.667,96.75 479,92.25C489.333,87.75 500.333,85.5 512,85.5C523.667,85.5 534.667,87.75 545,92.25C555.333,96.75 564.417,102.833 572.25,110.5C580.083,118.167 586.25,127.167 590.75,137.5C595.25,147.833 597.5,158.833 597.5,170.5L597.5,512C597.5,523.667 595.25,534.667 590.75,545C586.25,555.333 580.083,564.417 572.25,572.25C564.417,580.083 555.333,586.25 545,590.75C534.667,595.25 523.667,597.5 512,597.5C500.333,597.5 489.333,595.25 479,590.75C468.667,586.25 459.583,580.083 451.75,572.25C443.917,564.417 437.75,555.333 433.25,545C428.75,534.667 426.5,523.667 426.5,512ZM405.5,853.5C405.5,838.833 408.333,825 414,812C419.667,799 427.333,787.667 437,778C446.667,768.333 457.917,760.667 470.75,755C483.583,749.333 497.333,746.5 512,746.5C526.667,746.5 540.417,749.333 553.25,755C566.083,760.667 577.333,768.333 587,778C596.667,787.667 604.333,799 610,812C615.667,825 618.5,838.833 618.5,853.5C618.5,868.167 615.667,881.917 610,894.75C604.333,907.583 596.667,918.833 587,928.5C577.333,938.167 566,945.833 553,951.5C540,957.167 526.333,960 512,960C497.333,960 483.583,957.167 470.75,951.5C457.917,945.833 446.667,938.167 437,928.5C427.333,918.833 419.667,907.583 414,894.75C408.333,881.917 405.5,868.167 405.5,853.5Z"
                ></path>
            </svg>
        </span>
    </div>
    
    <div class="info-bar-content">
        <span><%= Title %></span>
        <p><%= Message %></p>
        <a href="<%= href %>" class="button style-hyperlink unindent"><%= AnchorText %></a>
    </div>
</div>

<style>
    .info-bar {
        align-items: center;
        background-clip: padding-box;
        border: 1px solid var(--wui-card-stroke-default);
        border-radius: var(--wui-control-corner-radius);
        box-sizing: border-box;
        display: flex;
        font-family: var(--wui-font-family-text);
        min-block-size: 48px;
        padding-inline-start: 15px;
        position: relative;
        user-select: none;
    }

    .info-bar.severity-caution {
        background-color: var(--wui-system-caution-background);
    }

    .info-bar-icon {
        align-self: flex-start;
        display: flex;
        flex: 0 0 auto;
        margin-block-start: 16px;
    }

    .info-badge {
        align-items: center;
        border-radius: 16px;
        box-sizing: border-box;
        color: var(--wui-text-on-accent-primary);
        display: inline-flex;
        font-family: var(--wui-font-family-small);
        font-size: var(--wui-font-size-caption);
        justify-content: center;
        line-height: var(--wui-font-size-caption);
        min-block-size: 16px;
        min-inline-size: 16px;
        padding: 2px 4px;
        user-select: none;
    }

    .info-badge.severity-caution {
        background-color: var(--wui-system-caution);
    }

    .info-bar-content {
        align-items: center;
        box-sizing: border-box;
        display: flex;
        flex: 1 1 auto;
        flex-wrap: wrap;
        margin-block-end: 15px;
        margin-block-start: 13px;
        margin-inline-start: 13px;
        position: relative;
    }

    .info-bar-content > span {
        color: var(--wui-text-primary);
        font-size: var(--wui-font-size-body-strong);
        font-weight: 600;
        line-height: 20px;
        margin: 0;
        margin-inline-end: 12px;
    }

    .info-bar-content > p {
        flex: 1 1 auto;
        margin-inline-end: 15px;
        color: var(--wui-text-primary);
        font-size: var(--wui-font-size-body);
        font-weight: 400;
        line-height: 20px;
        margin: 0;
    }

    svg {
    aspect-ratio: 1 / 1;
    width: 8px;
  }
</style>