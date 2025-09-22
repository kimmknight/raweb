# Translating RAWeb

This guide is for contributing translations to RAWeb.

## Translation File Locations and Formats

Translation files are located at `frontend/lib/public/locales/{{locale}}.json`. Some exist already, but additional languages can be added. Replace `{{locale}}` with the language code (e.g., `en-GB`, `fr`, `es`).

## How to Translate and Add Missing Keys

Often, new features are added, and the base language files are updated with new strings. These new keys will be missing from the translation files. Here's how to handle this:

### Identifying Missing Keys

- **In the Application:** If you are testing the application with a non-base language, you might see the base language string displayed instead of the translated string. This indicates a missing key.
- **Comparing Files:** You can compare the base language file (`en.json`) with the translation file for your language. Keys present in the base file but not in the translation file are missing.

### Adding and Translating Missing Keys

Once you've identified missing keys, follow these steps to add them to your translation file:

### For Main Application (.json files)

1.  Locate the `frontend/lib/public/locales/` directory.
2.  Find the `.json` file for the language you want to translate (or create it if it's a new language).
3.  Open the base language `.json` file (e.g., `en.json`) to find the new keys and their base language values.
4.  Add the missing keys to your language's `.json` file, maintaining the correct JSON structure.
5.  Translate the text value for each new key. **Do not change the key names.**
6.  Preserve any placeholders (like `{{name}}`, `{count}`) or HTML tags within the strings.

Example (`.json`):

```json
{
  "existing_key": "Existing Translation",
  "new_key": "New Translation for New Key" // add and translate the new key
}
```

## Submitting Your Translations

To submit your translations:

1.  Fork the [RAWeb repository](https://github.com/kimmknight/raweb).
2.  Clone your forked repository.
3.  Create a new branch for your translation work (e.g., `translate/fr`).
4.  Add your translated content, including any newly added keys, to the relevant `.json` file(s) (create a new file if necessary for a new language).
5.  Commit your changes with a clear message (e.g., `feat(i18n): add French login translations`, `feat(i18n): update Spanish translations with new keys`).
6.  Push your branch to your fork.
7.  Open a Pull Request to the main [RAWeb repository](https://github.com/kimmknight/raweb). Mention the language and which part(s) of the app you've translated in the PR description.

## Need Help?

If you have questions or need clarification on a string's context, or are unsure how to add missing keys, please open an issue on the [issue tracker](https://github.com/kimmknight/raweb/issues) with the label `i18n` or `translation`.

Thank you for your contribution!
