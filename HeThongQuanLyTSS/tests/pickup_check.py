"""Run against the existing test DB, with Smtp__DeliveryMode=PickupDirectory."""
import http.cookiejar, json, time, urllib.request
from pathlib import Path

client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def call(path, data=None):
    request = urllib.request.Request('http://localhost:5148/api'+path,
        data=json.dumps(data).encode() if data is not None else None,
        headers={'Content-Type':'application/json'})
    with client.open(request) as response:
        return json.load(response)

call('/auth/login', {'identity':'admin@test.local','password':'LocalTesting-2026!'})
profile = call('/people')[0]
call('/work/meetings', {'title':'Pickup verification','profileId':profile['id'],
    'start':'2026-10-15T02:00:00Z','end':'2026-10-15T03:00:00Z','detail':'Local email test only'})
for _ in range(10):
    jobs = call('/admin/mail')
    if any(j['status']=='SavedToPickup' and 'Pickup verification' in (j['subject']+j['body']) for j in jobs):
        folder = Path(__file__).resolve().parents[1]/'backend'/'Mã nguồn'/'CareerPortal.Api'/'App_Data'/'mail'
        assert list(folder.glob('*.eml')), 'No EML output'
        print('PASS: local email saved to .eml and marked SavedToPickup')
        break
    time.sleep(4)
else:
    raise AssertionError('Pickup email was not saved')
