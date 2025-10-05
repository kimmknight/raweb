'use strict';

const patternsConfig = require('./patterns.js');

/**
 * @typedef {import('markdown-it')} MarkdownIt
 *
 * @typedef {import('markdown-it/lib/rules_core/state_core.mjs').default} StateCore
 *
 * @typedef {import('markdown-it/lib/token.mjs').default} Token
 *
 * @typedef {import('markdown-it/lib/token.mjs').Nesting} Nesting
 *
 * @typedef {Object} Options
 * @property {!string} leftDelimiter left delimiter, default is `{`(left curly bracket)
 * @property {!string} rightDelimiter right delimiter, default is `}`(right curly bracket)
 * @property {AllowedAttribute[]} allowedAttributes empty means no limit
 *
 * @typedef {string|RegExp} AllowedAttribute rule of allowed attribute
 *
 * @typedef {[string, string]} AttributePair
 *
 * @typedef {[number, number]} SourceLineInfo
 *
 * @typedef {Object} CurlyAttrsPattern
 * @property {string} name
 * @property {DetectingRule[]} tests
 * @property {(tokens: Token[], i: number, j?: number) => void} transform
 *
 * @typedef {Object} MatchedResult
 * @property {boolean} match true means matched
 * @property {number?} j postion index number of Array<{@link Token}>
 *
 * @typedef {(str: string) => boolean} DetectingStrRule
 *
 * @typedef {Object} DetectingRule rule for testing {@link Token}'s properties
 * @property {number=} shift offset index number of Array<{@link Token}>
 * @property {number=} position fixed index number of Array<{@link Token}>
 * @property {(string | DetectingStrRule)=} type
 * @property {(string | DetectingStrRule)=} tag
 * @property {DetectingRule[]=} children
 * @property {(string | DetectingStrRule)=} content
 * @property {(string | DetectingStrRule)=} markup
 * @property {(string | DetectingStrRule)=} info
 * @property {Nesting=} nesting
 * @property {number=} level
 * @property {boolean=} block
 * @property {boolean=} hidden
 * @property {AttributePair[]=} attrs
 * @property {SourceLineInfo[]=} map
 * @property {any=} meta
 */

/** @type {Options} */
const defaultOptions = {
  leftDelimiter: '{',
  rightDelimiter: '}',
  allowedAttributes: []
};

/**
 * @param {MarkdownIt} md
 * @param {Options=} options_
 */
module.exports = function attributes(md, options_) {
  let options = Object.assign({}, defaultOptions);
  options = Object.assign(options, options_);

  const patterns = patternsConfig(options);

  /**
   * @param {StateCore} state
   */
  function curlyAttrs(state) {
    const tokens = state.tokens;

    for (let i = 0; i < tokens.length; i++) {
      for (let p = 0; p < patterns.length; p++) {
        const pattern = patterns[p];
        let j = null; // position of child with offset 0
        const match = pattern.tests.every(t => {
          const res = test(tokens, i, t);
          if (res.j !== null) { j = res.j; }
          return res.match;
        });
        if (match) {
          try {
            pattern.transform(tokens, i, j);
            if (pattern.name === 'inline attributes' || pattern.name === 'inline nesting 0') {
              // retry, may be several inline attributes
              p--;
            }
          } catch (error) {
            // eslint-disable-next-line no-console
            console.error(`markdown-it-attrs: Error in pattern '${pattern.name}': ${error.message}`);
            console.error(error.stack);
          }
        }
      }
    }
  }

  md.core.ruler.before('linkify', 'curly_attributes', curlyAttrs);
};

/**
 * Test if t matches token stream.
 *
 * @param {Token[]} tokens
 * @param {number} i
 * @param {DetectingRule} t
 * @returns {MatchedResult}
 */
function test(tokens, i, t) {
  /** @type {MatchedResult} */
  const res = {
    match: false,
    j: null  // position of child
  };

  const ii = t.shift !== undefined
    ? i + t.shift
    : t.position;

  if (t.shift !== undefined && ii < 0) {
    // we should never shift to negative indexes (rolling around to back of array)
    return res;
  }

  const token = get(tokens, ii);  // supports negative ii


  if (token === undefined) { return res; }

  for (const key of Object.keys(t)) {
    if (key === 'shift' || key === 'position') { continue; }

    if (token[key] === undefined) { return res; }

    if (key === 'children' && isArrayOfObjects(t.children)) {
      if (token.children.length === 0) {
        return res;
      }
      let match;
      /** @type {DetectingRule[]} */
      const childTests = t.children;
      /** @type {Token[]} */
      const children = token.children;
      if (childTests.every(tt => tt.position !== undefined)) {
        // positions instead of shifts, do not loop all children
        match = childTests.every(tt => test(children, tt.position, tt).match);
        if (match) {
          // we may need position of child in transform
          const j = last(childTests).position;
          res.j = j >= 0 ? j : children.length + j;
        }
      } else {
        for (let j = 0; j < children.length; j++) {
          match = childTests.every(tt => test(children, j, tt).match);
          if (match) {
            res.j = j;
            // all tests true, continue with next key of pattern t
            break;
          }
        }
      }

      if (match === false) { return res; }

      continue;
    }

    switch (typeof t[key]) {
    case 'boolean':
    case 'number':
    case 'string':
      if (token[key] !== t[key]) { return res; }
      break;
    case 'function':
      if (!t[key](token[key])) { return res; }
      break;
    case 'object':
      if (isArrayOfFunctions(t[key])) {
        const r = t[key].every(tt => tt(token[key]));
        if (r === false) { return res; }
        break;
      }
    // fall through for objects !== arrays of functions
    default:
      throw new Error(`Unknown type of pattern test (key: ${key}). Test should be of type boolean, number, string, function or array of functions.`);
    }
  }

  // no tests returned false -> all tests returns true
  res.match = true;
  return res;
}

function isArrayOfObjects(arr) {
  return Array.isArray(arr) && arr.length && arr.every(i => typeof i === 'object');
}

function isArrayOfFunctions(arr) {
  return Array.isArray(arr) && arr.length && arr.every(i => typeof i === 'function');
}

/**
 * Get n item of array. Supports negative n, where -1 is last
 * element in array.
 * @param {Token[]} arr
 * @param {number} n
 * @returns {Token=}
 */
function get(arr, n) {
  return n >= 0 ? arr[n] : arr[arr.length + n];
}

/**
 * get last element of array, safe - returns {} if not found
 * @param {DetectingRule[]} arr
 * @returns {DetectingRule}
 */
function last(arr) {
  return arr.slice(-1)[0] || {};
}
