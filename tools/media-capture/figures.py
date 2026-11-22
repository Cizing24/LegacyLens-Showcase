"""Generate figures from committed benchmark samples and actual project references."""
import json
from pathlib import Path
from xml.etree import ElementTree as ET
import matplotlib
matplotlib.use('Agg')
matplotlib.rcParams['svg.fonttype'] = 'none'
matplotlib.rcParams['svg.hashsalt'] = 'LegacyLens-SQL001'
import matplotlib.pyplot as plt

root = Path(__file__).resolve().parents[2]
media = root / 'docs/media'
media.mkdir(parents=True, exist_ok=True)
reports = [json.loads((root / f'docs/benchmarks/{scale}/results.json').read_text(encoding='utf-8-sig')) for scale in [10000, 50000, 100000]]
fig, axes = plt.subplots(1, 2, figsize=(12, 4.8), layout='constrained')
colors = ['#8c3c2b', '#a97721', '#155976']
for i, (stage, label) in enumerate([('baseline', 'Baseline'), ('rewrite-only', 'Rewrite only'), ('indexed', 'Rewrite + indexes')]):
    values = [next(result for result in report['results'] if result['stage'] == stage) for report in reports]
    axes[0].plot([10, 50, 100], [value['p95Ms'] for value in values], marker='o', label=label, color=colors[i], linewidth=2)
    bars = axes[1].bar(i, values[-1]['logicalReads'], color=colors[i], width=.6)
    axes[1].bar_label(bars, padding=4)
axes[0].set(xlabel='Public records (thousands)', ylabel='P95 latency (ms)', title='Repeated warm-query latency', xticks=[10, 50, 100], ylim=(0, None))
axes[0].legend(frameon=False)
axes[1].set(title='100k: total logical reads', ylabel='8 KB pages read', xticks=[0, 1, 2], xticklabels=['Baseline', 'Rewrite', 'Indexed'], ylim=(0, 3900))
for ax in axes:
    ax.spines[['top', 'right']].set_visible(False)
    ax.grid(axis='y', alpha=.16)
    ax.set_axisbelow(True)
fig.suptitle('LegacyLens / SQL-001 — actual SQL Server measurements', fontsize=15, fontweight='bold')
fig.text(.5, -.015, '5 warmups + 40 measured runs per arm / single local client / no cold-cache or production claims', ha='center', fontsize=9)
fig.savefig(media / 'sql-benchmark.png', dpi=180, bbox_inches='tight')
fig.savefig(media / 'sql-benchmark.svg', bbox_inches='tight')
svg_path = media / 'sql-benchmark.svg'
svg_path.write_text('\n'.join(line.rstrip() for line in svg_path.read_text(encoding='utf-8').splitlines()) + '\n', encoding='utf-8')
plt.close(fig)

ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
projects = {}
for path in sorted((root / 'src').glob('*/*.csproj')):
    tree = ET.parse(path)
    projects[path.stem] = [node.find('m:Name', ns).text for node in tree.findall('.//m:ProjectReference', ns)]
assert set(projects) == {'LegacyLens.Web', 'LegacyLens.Application', 'LegacyLens.Infrastructure', 'LegacyLens.Domain'}
for source in ['src/LegacyLens.Web/SecurePage.cs', 'src/LegacyLens.Application/CaseService.cs', 'src/LegacyLens.Application/TransferRules.cs', 'src/LegacyLens.Infrastructure/CaseStore.cs', 'src/LegacyLens.Infrastructure/TransferStore.cs']:
    assert (root / source).is_file(), source
(root / 'docs/architecture').mkdir(parents=True, exist_ok=True)
(root / 'docs/architecture/project-references.json').write_text(json.dumps(projects, indent=2), encoding='utf-8')

svg = '''<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="660" viewBox="0 0 1200 660" role="img" aria-labelledby="title desc">
<title id="title">LegacyLens actual architecture</title><desc id="desc">Browser submits real Web Forms pages. Application rules call ADO.NET stores through ICaseStore. SQL Server commits case, history and audit together. Domain is shared. NUnit and Playwright verify the system.</desc>
<defs><marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L9,3 z" fill="#155976"/></marker></defs>
<rect width="1200" height="660" fill="#f3f6f7"/><g font-family="Segoe UI,Arial,sans-serif" fill="#182c3b"><text x="48" y="56" font-size="27" font-weight="700">LegacyLens — code-grounded architecture</text><text x="48" y="87" font-size="15" fill="#576b79">.NET Framework 4.8 / IIS Express / SQL Server LocalDB · local engineering case study</text>
<g fill="#fff" stroke="#ccd9df"><rect x="48" y="145" width="185" height="150" rx="8"/><rect x="290" y="145" width="245" height="150" rx="8"/><rect x="593" y="145" width="245" height="150" rx="8"/><rect x="895" y="145" width="255" height="150" rx="8"/><rect x="290" y="355" width="548" height="92" rx="8"/><rect x="48" y="510" width="1102" height="98" rx="8"/><rect x="895" y="330" width="255" height="140" rx="8"/></g>
<g font-size="20" font-weight="600"><text x="70" y="180">Browser</text><text x="312" y="180">LegacyLens.Web</text><text x="615" y="180">Application rules</text><text x="917" y="180">ADO.NET stores</text><text x="312" y="389">LegacyLens.Domain</text><text x="70" y="545">Verification &amp; evidence</text></g>
<g font-size="15"><text x="70" y="213">Server-rendered forms</text><text x="70" y="240">Actual POST / ViewState</text><text x="70" y="267">Responsive CSS</text><text x="312" y="213">Login / Cases / Customers</text><text x="312" y="240">SecurePage / Session key</text><text x="312" y="267">Code-behind orchestration</text><text x="615" y="213">CaseService → ICaseStore</text><text x="615" y="240">CaseRules / TransferRules</text><text x="615" y="267">Authorization / validation</text><text x="917" y="213">CaseStore / TransferStore</text><text x="917" y="240">Identity / Customer stores</text><text x="917" y="267">Typed SQL parameters</text><text x="312" y="419">Actor · CaseRecord · Customer · TransferRequest · ConflictException</text><text x="70" y="576">NUnit: rules / transactions / concurrency / migrations   ·   Playwright: real IIS flows   ·   SQL: plans / reads / repeated samples</text></g>
<g stroke="#155976" stroke-width="2" fill="none" marker-end="url(#arrow)"><path d="M233 220 H282"/><path d="M535 220 H585"/><path d="M838 220 H887"/><path d="M715 295 V346"/><path d="M1020 295 V322"/></g>
<text x="915" y="362" font-size="18" font-weight="600">SQL Server / LocalDB</text><text x="915" y="393" font-size="14">Cases + History + Audit</text><text x="915" y="422" font-size="14">Atomic SQL transaction</text><text x="915" y="448" font-size="13">rowversion / request key</text><text x="48" y="643" font-size="12" fill="#576b79">Verified against .csproj references and named source files; full dependency list: docs/architecture/project-references.json</text></g></svg>'''
(media / 'architecture.svg').write_text(svg, encoding='utf-8')
latest = reports[-1]['results']
rows = ''.join(f"<tr><td>{r['stage']}</td><td>{r['p50Ms']:.4f}</td><td>{r['p95Ms']:.4f}</td><td>{r['logicalReads']:,}</td></tr>" for r in latest)
(media / 'evidence.html').write_text('''<!doctype html><html lang="zh-Hant"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>LegacyLens 工程實測證據</title><style>body{font:18px/1.6 'Segoe UI','Microsoft JhengHei',sans-serif;margin:40px;color:#182c3b;background:#f3f6f7}h1{font-size:30px}img{max-width:100%;max-height:460px}table{border-collapse:collapse;background:white;width:100%}td,th{padding:10px 20px;text-align:left;border-bottom:1px solid #dbe3e7}p{color:#576b79}a{color:#155976}</style><h1>SQL-001：100,000 筆公開案件實測</h1><p>真正 SQL Server 查詢計畫、Logical Reads 與重複計時；每方案 5 次預熱＋40 次實測。</p><img src="sql-benchmark.png" alt="P95 與 logical reads 實測圖表"><table><tr><th>方案</th><th>P50 ms</th><th>P95 ms</th><th>Logical Reads</th></tr>''' + rows + '''</table><p>暖快取、單一本機 client；結果等價檢查 offsets 0 / 20 / 200。非 SLA 或企業導入效益。原始資料：docs/benchmarks/100000/results.json。</p><p><a href="architecture.svg">檢視實際程式架構</a></p></html>''', encoding='utf-8')
print('Generated benchmark plots, architecture SVG and evidence page from actual source/results.')
