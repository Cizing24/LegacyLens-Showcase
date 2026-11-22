"""Check staged/current files and every local Git object without printing private values."""
import json
import os
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EMAIL = re.compile(rb'\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b')
USER_PATH = re.compile(rb'(?i)[a-z]:[\\/]Users[\\/][^\s<>"\']+')

def findings(data):
    result = []
    # Preserve byte boundaries; dropping undecodable bytes can invent email matches in media.
    texts = [data] + [match.group().decode('utf-16-le').encode() for match in re.finditer(rb'(?:[\x20-\x7e]\x00){5,}', data)]
    for text in texts:
        if USER_PATH.search(text):
            result.append('personal-user-path')
        for match in EMAIL.finditer(text):
            if not match.group().endswith((b'@users.noreply.github.com', b'@example.com')):
                result.append('nonpublic-email')
        for key in ['COMPUTERNAME', 'USERNAME']:
            value = os.environ.get(key, '')
            if len(value) >= 4 and value.casefold().encode() in text.lower():
                result.append('local-' + key.lower())
    return sorted(set(result))

def git(*args, **kwargs):
    return subprocess.check_output(['git', *args], cwd=ROOT, **kwargs)

def verify():
    problems = []
    paths = list(filter(None, git('ls-files', '-z').decode().split('\0')))
    for relative in paths:
        if re.search(r'(?i)(?:^|/)(?:\.runtime|node_modules|bin|obj)/|(?:\.har|\.pfx|\.p12|\.mdf|\.ldf|trace\.zip)$', relative):
            problems.append({'path': relative, 'kind': 'private-runtime-artifact'})
        for kind in findings((ROOT / relative).read_bytes()):
            problems.append({'path': relative, 'kind': kind})
    headers = git('cat-file', '--batch-all-objects', '--batch-check=%(objectname) %(objecttype)').decode().splitlines()
    counts = {}
    commits = []
    for header in headers:
        sha, kind = header.split()
        counts[kind] = counts.get(kind, 0) + 1
        if kind == 'tag':
            problems.append({'object': sha, 'kind': 'unexpected-tag-object'})
        if kind in ['blob', 'commit', 'tag']:
            data = git('cat-file', kind, sha)
            for category in findings(data):
                problems.append({'object': sha, 'kind': category})
            if kind == 'commit':
                commits.append(sha)
                for line in data.splitlines():
                    if line.startswith((b'author ', b'committer ')):
                        if not re.search(rb'<[^<>]+@users\.noreply\.github\.com>', line):
                            problems.append({'object': sha, 'kind': 'non-noreply-identity'})
    reachable = set(git('rev-list', '--all').decode().splitlines())
    if set(commits) != reachable:
        problems.append({'kind': 'unreachable-commit-object'})
    roots = git('rev-list', '--max-parents=0', '--all').decode().splitlines()
    if len(roots) != 1:
        problems.append({'kind': 'expected-one-independent-root'})
    remotes = git('remote', '-v').decode()
    if remotes and any('Cizing24/LegacyLens-Showcase' not in line for line in remotes.splitlines()):
        problems.append({'kind': 'unexpected-remote'})
    summary = {'trackedFiles': len(paths), 'objects': counts, 'independentRoots': len(roots), 'findings': problems}
    output = ROOT / '.runtime/security/public-object-review.json'
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps(summary, indent=2))
    assert not problems, 'Public privacy checks failed; see the ignored review report.'
    return summary

if __name__ == '__main__':
    verify()
