<%@ Control Language="C#" AutoEventWireup="true" %>

<script runat="server">
    public string Message {get; set; }
    public string style {get; set; }
</script>

<div class="info-bar severity-critical" role="alert" style="<%= style %>">
    <div class="info-bar-icon">
        <span class="info-badge severity-critical svelte-106nxdf">
            <svg viewBox="172 171 683 683" aria-hidden="true" xmlns="http://www.w3.org/2000/svg">
                <path fill="currentColor"
                    d="M512.5,587.5L262.5,838C252.167,848.333 239.5,853.5 224.5,853.5C209.5,853.5 196.917,848.417 186.75,838.25C176.583,828.083 171.5,815.5 171.5,800.5C171.5,785.5 176.667,772.833 187,762.5L437,512L187,262C176.667,251.667 171.5,239.167 171.5,224.5C171.5,217.167 172.833,210.167 175.5,203.5C178.167,196.833 181.917,191.167 186.75,186.5C191.583,181.833 197.167,178.083 203.5,175.25C209.833,172.417 216.833,171 224.5,171C239.167,171 251.667,176.167 262,186.5L512.5,437L762.5,186.5C773.167,175.833 785.833,170.5 800.5,170.5C807.833,170.5 814.75,171.917 821.25,174.75C827.75,177.583 833.417,181.417 838.25,186.25C843.083,191.083 846.833,196.75 849.5,203.25C852.167,209.75 853.5,216.667 853.5,224C853.5,238.667 848.333,251.167 838,261.5L587.5,512L838,762.5C848.667,773.167 854,785.833 854,800.5C854,807.833 852.583,814.667 849.75,821C846.917,827.333 843.083,832.917 838.25,837.75C833.417,842.583 827.75,846.417 821.25,849.25C814.75,852.083 807.833,853.5 800.5,853.5C785.5,853.5 772.833,848.333 762.5,838Z">
                </path>
            </svg>
        </span>
    </div>
    <div class="info-bar-content">
        <p><%= Message %></p>
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

    .info-bar.severity-critical {
        background-color: var(--wui-system-critical-background);
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
        font-size: var(--wui-caption-font-size);
        justify-content: center;
        line-height: var(--wui-caption-font-size);
        min-block-size: 16px;
        min-inline-size: 16px;
        padding: 2px 4px;
        user-select: none;
    }

    .info-badge.severity-critical {
        background-color: var(--wui-system-critical);
    }

    .info-bar-content {
        align-items: center;
        box-sizing: border-box;
        display: flex;
        flex: 1 1 auto;
        flex-wrap: wrap;
        margin-block-end: 7px;
        margin-block-start: 7px;
        margin-inline-start: 13px;
        position: relative;
    }

    .info-bar-content > p {
        flex: 1 1 auto;
        margin-inline-end: 15px;
        color: var(--wui-text-primary);
        font-size: var(--wui-body-font-size);
        font-weight: 400;
        line-height: 20px;
        margin: 0;
    }
</style>