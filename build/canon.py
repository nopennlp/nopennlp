#!/usr/bin/env python3
"""Canonicalize OpenNLP CLI output so two implementations can be compared on content.

Java orders a tool's options by Class.getMethods() and its formats by HashMap iteration.
The JDK specifies neither, so both are implementation details rather than contract; the
port uses declaration order and registration order instead. This sorts both sides so a
diff reports genuine differences in options, value names, descriptions and formats.
"""
import re
import sys


def canonical_usage(line):
    """Sort the [.fmt|.fmt] alternation and the option list on a Usage line."""
    m = re.match(r'^(Usage: \S+ \S+?)(\[\.[^\]]*\])?(\s.*)$', line)
    if not m or '-' not in (m.group(3) or ''):
        return line

    head, fmts, rest = m.group(1), m.group(2) or '', m.group(3)

    if fmts:
        fmts = '[' + '|'.join(sorted(fmts[1:-1].split('|'))) + ']'

    # An option is "-name value", optionally wrapped in brackets when optional.
    opts = re.findall(r'\[-\w+(?: [^\[\]]+?)?\]|-\w+(?: \S+)?', rest)
    opts = [o.strip() for o in opts if o.strip()]

    return head + fmts + ' ' + ' '.join(sorted(opts))


def canonical_description_block(block):
    """Sort the 'Arguments description:' entries, each a name line plus optional detail."""
    entries, current = [], []

    for line in block:
        if not line.startswith('\t\t'):
            if current:
                entries.append(current)
            current = [line]
        else:
            current.append(line)

    if current:
        entries.append(current)

    return [line for entry in sorted(entries) for line in entry]


def main():
    out, block = [], []

    for line in sys.stdin.read().split('\n'):
        if line.startswith('\t'):
            block.append(line)
            continue

        if block:
            out.extend(canonical_description_block(block))
            block = []

        out.append(canonical_usage(line))

    if block:
        out.extend(canonical_description_block(block))

    sys.stdout.write('\n'.join(out))


if __name__ == '__main__':
    main()
