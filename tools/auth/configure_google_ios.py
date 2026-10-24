#!/usr/bin/env python3
"""Owner-run configuration using real OAuth IDs, never generated placeholders."""
import argparse
import pathlib
import plistlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('--ios-client-id', required=True)
parser.add_argument('--web-client-id', required=True)
args = parser.parse_args()
for value in (args.ios_client_id, args.web_client_id):
    if not re.fullmatch(r'[0-9]+-[A-Za-z0-9_-]+\.apps\.googleusercontent\.com', value):
        parser.error('Provide the actual Google Cloud OAuth client ID.')
path = pathlib.Path(__file__).resolve().parents[2] / 'mobile/ios/Runner/Info.plist'
with path.open('rb') as stream:
    config = plistlib.load(stream)
config['GIDClientID'] = args.ios_client_id
config['GIDServerClientID'] = args.web_client_id
reversed_id = '.'.join(reversed(args.ios_client_id.split('.')))
entries = config.setdefault('CFBundleURLTypes', [])
if not any(reversed_id in entry.get('CFBundleURLSchemes', []) for entry in entries):
    entries.append({'CFBundleTypeRole': 'Editor', 'CFBundleURLSchemes': [reversed_id]})
with path.open('wb') as stream:
    plistlib.dump(config, stream, sort_keys=False)
print('Configured iOS Google client and URL scheme. Review the Info.plist diff.')
