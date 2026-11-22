"""Download only non-location categorical fields from the official NYC Socrata API."""
import csv
import hashlib
import json
from pathlib import Path
from datetime import datetime, timezone
from urllib.parse import urlencode
from urllib.request import urlopen, Request
import argparse
import time

FIELDS = ['unique_key', 'created_date', 'complaint_type', 'status', 'agency']
ENDPOINT = 'https://data.cityofnewyork.us/resource/erm2-nwe9.json'

def fetch(url):
    for attempt in range(4):
        try:
            with urlopen(Request(url, headers={'User-Agent': 'LegacyLens engineering dataset sampler'}), timeout=120) as response:
                return json.load(response)
        except Exception:
            if attempt == 3:
                raise
            time.sleep(2 ** attempt)

def download(limit):
    if limit < 1 or limit > 100000:
        raise ValueError('Sample size must be between 1 and 100000')
    target = Path('.runtime/data')
    target.mkdir(parents=True, exist_ok=True)
    rows = []
    queries = []
    seen = set()
    for offset in range(0, limit, 10000):
        query = {'$select': ','.join(FIELDS), '$where': "created_date >= '2024-01-01T00:00:00' AND created_date < '2024-02-01T00:00:00'", '$order': 'created_date ASC,unique_key ASC', '$limit': min(10000, limit - offset), '$offset': offset}
        url = ENDPOINT + '?' + urlencode(query)
        batch = fetch(url)
        for row in batch:
            assert set(row).issubset(FIELDS), 'Unexpected fields must never be persisted'
            key = row['unique_key']
            if key in seen:
                raise ValueError('Duplicate source key: source changed during pagination; retry snapshot')
            seen.add(key)
            rows.append(row)
        queries.append(url)
        print(f'Downloaded {len(rows)} allowlisted service requests', flush=True)
        if len(batch) < query['$limit']:
            break
    raw = target / 'nyc-311-snapshot.json'
    raw.write_text(json.dumps(rows, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
    csv_path = target / 'nyc-311.csv'
    with csv_path.open('w', encoding='utf-8-sig', newline='') as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)
    metadata = fetch('https://data.cityofnewyork.us/api/views/erm2-nwe9.json')
    manifest = {
        'classification': 'Public Dataset Observation', 'datasetId': 'erm2-nwe9', 'source': ENDPOINT,
        'retrievedAtUtc': datetime.now(timezone.utc).isoformat(), 'sourceRowsUpdatedAt': metadata.get('rowsUpdatedAt'),
        'requestedRows': limit, 'downloadedRows': len(rows), 'uniqueKeys': len(seen), 'fields': FIELDS,
        'window': '2024-01-01 inclusive through 2024-02-01 exclusive; earliest rows, not a representative random sample',
        'queries': queries, 'snapshotSha256': hashlib.sha256(raw.read_bytes()).hexdigest(),
        'csvSha256': hashlib.sha256(csv_path.read_bytes()).hexdigest(),
        'sourceLicenseId': metadata.get('licenseId'),
        'terms': 'https://www.nyc.gov/home/terms-of-use.page',
        'columns': [{key: col.get(key) for key in ['fieldName', 'name', 'dataTypeName', 'description']} for col in metadata['columns'] if col.get('fieldName') in FIELDS],
        'privacy': 'No address, ZIP, coordinates, free-form resolution text, names or contact fields requested or stored'
    }
    Path('docs/datasets').mkdir(parents=True, exist_ok=True)
    Path('docs/datasets/snapshot-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    assert len(rows) == len(seen)
    return manifest

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--limit', type=int, default=100000)
    print(json.dumps(download(parser.parse_args().limit), ensure_ascii=False, indent=2))
