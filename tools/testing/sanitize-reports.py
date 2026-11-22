"""Create public report copies while preserving test outcomes, assertions and measurements."""
import json
import re
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
EMAIL = re.compile(r'\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b')
WORKSPACE = re.compile(r'(?i)[a-z]:[\\/]Users[\\/][^\n\r"<>]*?[\\/]LegacyLens(?=[\\/\s"<>]|$)')

def clean(value):
    if isinstance(value, dict):
        return {key: clean(item) for key, item in value.items()}
    if isinstance(value, list):
        return [clean(item) for item in value]
    if isinstance(value, str):
        value = WORKSPACE.sub('<workspace>', value)
        return EMAIL.sub(lambda match: match.group() if match.group().endswith('@users.noreply.github.com') else '[redacted-email]', value)
    return value

def sanitize(path):
    path = path.resolve()
    assert path.is_relative_to(ROOT / 'docs/testing') or path == ROOT / 'docs/media/capture-manifest.json', 'Only public test reports and capture manifest may be sanitized'
    original = path.read_bytes()
    text = original.decode('utf-8-sig')
    if path.suffix == '.json':
        document = json.loads(text)
        transformed = clean(document)
        if path.name == 'capture-manifest.json':
            transformed['videoSource'] = '.runtime/media/' + re.split(r'[\\/]', document['videoSource'])[-1]
        if 'stats' in document:
            assert document['stats'] == transformed['stats'], 'Timing/outcome stats must be unchanged'
        result = json.dumps(transformed, ensure_ascii=False, indent=2) + '\n'
    elif path.suffix == '.xml':
        root = ET.fromstring(text)
        outcomes = [(node.attrib.get('fullname'), node.attrib.get('result')) for node in root.iter('test-case')]
        for node in root.iter():
            for key in list(node.attrib):
                if node.tag == 'environment' and key in ['user', 'user-domain', 'machine-name']:
                    node.attrib[key] = '[redacted-host-identity]'
                else:
                    node.attrib[key] = clean(node.attrib[key])
            if node.text:
                node.text = clean(node.text)
        assert outcomes == [(node.attrib.get('fullname'), node.attrib.get('result')) for node in root.iter('test-case')]
        result = '<?xml version="1.0" encoding="utf-8"?>\n<!-- Public copy: local paths and identity metadata redacted; test outcomes retained. -->\n' + ET.tostring(root, encoding='unicode') + '\n'
    else:
        raise ValueError('Unsupported public report format')
    if result.encode() != original:
        archive = ROOT / '.runtime/security/original-reports' / path.relative_to(ROOT)
        archive.parent.mkdir(parents=True, exist_ok=True)
        if not archive.exists():
            archive.write_bytes(original)
        path.write_text(result, encoding='utf-8')
    return path.relative_to(ROOT).as_posix()

if __name__ == '__main__':
    files = [ROOT / arg for arg in sys.argv[1:]] if len(sys.argv) > 1 else [*ROOT.glob('docs/testing/*.json'), *ROOT.glob('docs/testing/*.xml'), ROOT / 'docs/media/capture-manifest.json']
    print(json.dumps({'sanitizedPublicCopies': [sanitize(path) for path in files]}, indent=2))
