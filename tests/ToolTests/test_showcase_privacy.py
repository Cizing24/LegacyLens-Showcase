import importlib.util
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('privacy', Path(__file__).resolve().parents[2] / 'tools/security/verify-public.py')
privacy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(privacy)

class ShowcasePrivacyTests(unittest.TestCase):
    def test_private_email_is_detected_in_plain_and_utf16(self):
        sample = 'private' + '@' + 'mail.invalid'
        for encoding in ['utf-8', 'utf-16-le']:
            self.assertIn('nonpublic-email', privacy.findings(sample.encode(encoding)))
        self.assertNotIn('nonpublic-email', privacy.findings(b'private\xff' + b'@mail.invalid'))

    def test_personal_windows_path_is_detected(self):
        for separator in ['/', '\\']:
            sample = separator.join(['C:', 'Users', 'example', 'work', 'report.xml'])
            self.assertIn('personal-user-path', privacy.findings(sample.encode()))

    def test_noreply_and_synthetic_example_are_allowed(self):
        self.assertEqual(privacy.findings(b'123+example@users.noreply.github.com test@example.com'), [])

if __name__ == '__main__':
    unittest.main()
