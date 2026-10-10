"""Offline cross-file checks; no database, credentials or network access."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
sql = '\n'.join((root / path).read_text(encoding='utf-8-sig') for path in ['Database/001_D01.sql','Database/003_D02_D04.sql','Database/004_D05_D06.sql'])
tables = re.findall(r'CREATE TABLE (\w+) \((.*?)\n\);', sql, re.S)
for table, body in tables:
    columns = set(re.findall(r'^    (\w+) (?:NUMBER|NVARCHAR2|VARCHAR2|DATE)\b', body, re.M))
    dto = (root / 'DTO/Tables' / (table + '.cs')).read_text(encoding='utf-8-sig')
    properties = set(re.findall(r'public [\w?]+ (\w+) \{ get; set; \}', dto))
    assert columns == properties, (table, columns ^ properties)
assert len(tables) == 14
for file in root.glob('*/*.csproj'):
    tree = ET.parse(file)
    ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
    assert tree.find('.//m:TargetFrameworkVersion', ns).text == 'v4.8'
    if file.parent.name != 'Web':
        assert not any('Kendo' in (x.get('Include') or '') for x in tree.findall('.//m:Reference', ns))
assert '5.2.7' in (root / 'Web/packages.config').read_text(encoding='utf-8-sig')
print('PASS 14 Oracle table/DTO contracts, three project targets, layer references and company MVC pin')
