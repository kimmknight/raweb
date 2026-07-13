---
title: Including screenshots in documentation
nav_title: Screenshots in documentation
---

When it makes sense, include screenshots in the documentation. Screenshots can enhance the documentation by showing users what to expect when they follow the instructions. They can also help clarify instructions that may be confusing when described with text alone.

When including screenshots in documentation, keep the following guidelines in mind:

- When annotating, use 3-pixel wide red boxes.
- Avoid using arrows or text annotations on screenshots.
- Only include screenshots that are saved in 30% quality WEBP format to reduce file size while maintaining readability.
- Use the `screenshot` CSS class on all screenshots to ensure consistent styling.
- Specify alt text for accessibility.
- Always specify a width. Never specify a height. RAWeb's documentation site will automatically calculate the appropriate height to maintain the aspect ratio of the image and prevent content layout shifts as the image loads.
- If a screenshot shows multiple steps, annotate them with numbered boxes. Outline the area that should be clicked with a 3px red box and place the number directly next to the box. Always start numbering at 1. Always use **Cascadia Code** font with size **18** and red font.

We recommend using Paint.NET for editing and saving screenshots. Paint.NET is free and has built-in support for saving in WEBP format. To save a screenshot in Paint.NET as a 30% quality WEBP, follow these steps:

1. Open the image in Paint.NET.
2. Click File > Save As.
3. In the Save As dialog, select "WEBP (\*.webp)" from the "Save as type" dropdown menu.
4. Click Save.
5. In the "Save Configuration" dialog that appears, set the "Quality" slider to 30 and click OK.
