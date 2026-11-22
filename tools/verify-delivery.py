"""Check deliverable links, executed reports, plans and media without rerunning expensive tests."""
import json
import re
import struct
import subprocess
from pathlib import Path
from urllib.parse import unquote
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]

def verify():
    checked = 0
    for document in [ROOT / 'README.md', ROOT / 'README.en.md', *ROOT.glob('docs/**/*.md')]:
        text = document.read_text(encoding='utf-8-sig')
        links = re.findall(r'\[[^\]]*\]\(([^)]+)\)', text)
        links += re.findall(r'<img[^>]+src=["\']([^"\']+)', text)
        for link in links:
            if re.match(r'^(https?://|mailto:|#)', link):
                continue
            target = unquote(link.split('#', 1)[0].split(' "', 1)[0])
            if not target:
                continue
            assert (document.parent / target).is_file(), f'Broken local link: {document.relative_to(ROOT)} -> {target}'
            checked += 1
    for name in ['final-nunit.xml', 'ci-nunit.xml']:
        report = ET.parse(ROOT / 'docs/testing' / name).getroot()
        assert report.attrib['result'] == 'Passed' and int(report.attrib['total']) == 54
        assert int(report.attrib['failed']) == 0 and int(report.attrib.get('skipped', 0)) == 0
    for name in ['final-e2e.json', 'ci-e2e.json']:
        stats = json.loads((ROOT / 'docs/testing' / name).read_text(encoding='utf-8-sig'))['stats']
        assert stats['expected'] == 11 and stats['unexpected'] == 0 and stats['skipped'] == 0
    ci_files = ['showcase-ci-nunit.xml', 'showcase-ci-e2e.json']
    ci_present = [(ROOT / 'docs/testing' / name).exists() for name in ci_files]
    assert ci_present[0] == ci_present[1], 'Showcase CI evidence must contain both reports'
    for name in ['readiness-nunit.xml', 'readiness-ci-nunit.xml', 'showcase-nunit.xml'] + (ci_files[:1] if all(ci_present) else []):
        root = ET.parse(ROOT / 'docs/testing' / name).getroot()
        assert root.attrib['result'] == 'Passed' and int(root.attrib['total']) == 58
        assert int(root.attrib['failed']) == 0 and int(root.attrib.get('skipped', 0)) == 0
        parents = {child: parent for parent in root.iter() for child in parent}
        units = integrations = 0
        for case in root.iter('test-case'):
            node = case
            integration = False
            while node is not None:
                integration |= any(prop.attrib.get('name') == 'Category' and prop.attrib.get('value') == 'Integration' for prop in node.findall('./properties/property'))
                node = parents.get(node)
            integrations += int(integration)
            units += int(not integration)
        assert (units, integrations) == (25, 33)
    for name in ['readiness-e2e.json', 'readiness-ci-e2e.json', 'showcase-e2e.json'] + (ci_files[1:] if all(ci_present) else []):
        report = json.loads((ROOT / 'docs/testing' / name).read_text(encoding='utf-8-sig'))
        stats = report['stats']
        assert stats['expected'] == 13 and stats['unexpected'] == 0 and stats['skipped'] == 0
        cases = json.dumps(report, ensure_ascii=False)
        assert 'historical assignee after Disable' in cases and 'historical assignee after Demote' in cases
    red = json.loads((ROOT / 'docs/testing/web001-red.json').read_text(encoding='utf-8-sig'))
    green = json.loads((ROOT / 'docs/testing/web001-green.json').read_text(encoding='utf-8-sig'))
    assert red['stats']['unexpected'] == 1 and green['stats']['expected'] == 1
    assert 'toHaveValue' in json.dumps(red), 'The red result must be draft retention, not an arbitrary error'
    for scale in [10000, 50000, 100000]:
        directory = ROOT / f'docs/benchmarks/{scale}'
        report = json.loads((directory / 'results.json').read_text(encoding='utf-8-sig'))
        assert report['sourceRows'] == scale
        for result in report['results']:
            assert result['iterations'] == len(result['samplesMs']) == 40
            assert result['warmups'] == 5 and result['resultEquivalent'] is True and result['logicalReads'] > 0
            plan = ET.parse(directory / (result['stage'] + '.sqlplan')).getroot()
            assert plan.tag.endswith('ShowPlanXML')
    for screenshot in (ROOT / 'docs/media').glob('*.png'):
        data = screenshot.read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        width, height = struct.unpack('>II', data[16:24])
        assert width >= 390 and height >= 400, screenshot.name
    for name, minimum, maximum in [('demo.mp4', 110, 120), ('workflow.gif', 10, 30)]:
        file = ROOT / 'docs/media' / name
        assert 0 < file.stat().st_size < 8 * 1024 * 1024
        probe = subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'json', str(file)], check=True, capture_output=True, text=True)
        duration = float(json.loads(probe.stdout)['format']['duration'])
        assert minimum <= duration <= maximum, (name, duration)
    manifest = json.loads((ROOT / 'docs/media/capture-manifest.json').read_text(encoding='utf-8-sig'))
    assert manifest['pageErrors'] == []
    patterns = [r'gho_[A-Za-z0-9]{30,}', r'github_pat_[A-Za-z0-9_]{30,}', r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----']
    for folder in ['src', 'tools', '.github']:
        for source in (ROOT / folder).rglob('*'):
            if not source.is_file() or any(part in ['bin', 'obj', 'graphify-out'] for part in source.parts):
                continue
            if source.suffix not in ['.cs', '.config', '.ps1', '.py', '.js', '.yml']:
                continue
            text = source.read_text(encoding='utf-8-sig')
            for pattern in patterns:
                assert re.search(pattern, text) is None, f'Credential pattern found: {source}'
    return {'localLinksChecked': checked, 'showcaseLocalNunit': 58, 'unit': 25, 'sqlIntegration': 33, 'showcaseLocalE2E': 13, 'showcaseCiReportsValidated': all(ci_present), 'sourceHistoricalReportsValidated': True, 'benchmarkScales': [10000, 50000, 100000], 'web001RedGreen': True, 'mediaValidated': True, 'credentialPatternScan': 'no matches'}

if __name__ == '__main__':
    result = verify()
    (ROOT / 'docs/testing/delivery-checks.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
