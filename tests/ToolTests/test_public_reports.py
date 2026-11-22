import importlib.util
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('sanitizer', Path(__file__).resolve().parents[2] / 'tools/testing/sanitize-reports.py')
sanitizer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sanitizer)

class PublicReportTests(unittest.TestCase):
    def test_windows_workspace_becomes_relative_without_changing_assertion(self):
        workspace = 'C:' + '/Users/example/Desktop/LegacyLens'
        self.assertEqual(sanitizer.clean(workspace + '/tests/test.cs'), '<workspace>/tests/test.cs')
        self.assertEqual(sanitizer.clean(workspace.replace('/', '\\') + '\\tests\\test.cs'), '<workspace>\\tests\\test.cs')

    def test_outcomes_and_numbers_are_preserved_in_nested_reports(self):
        value = {'stats': {'expected': 13, 'unexpected': 0, 'duration': 1234.5}, 'suites': [{'result': 'passed', 'email': 'test@example.com'}]}
        cleaned = sanitizer.clean(value)
        self.assertEqual(cleaned['stats'], value['stats'])
        self.assertEqual(cleaned['suites'][0]['result'], 'passed')
        self.assertEqual(cleaned['suites'][0]['email'], '[redacted-email]')

    def test_public_noreply_identity_is_retained(self):
        email = '123+example@users.noreply.github.com'
        self.assertEqual(sanitizer.clean(email), email)

if __name__ == '__main__':
    unittest.main()
