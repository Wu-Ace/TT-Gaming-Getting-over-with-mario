import subprocess,json,threading,queue,sys,time,os
from pathlib import Path
r=Path(r'E:\TT Gaming\TT-Gaming-Getting-over-with-mario');out=r/'scripts'/os.environ.get('AUDIO_EVIDENCE_DIR','foley-integration-20261109-172027')
p=subprocess.Popen([r'E:\Tuanjie Cowork\cli\bin\win32-x64\codely.exe','serve','unity-mcp','--stdio','--unity-project-path',str(r)],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True,encoding='utf-8')
q=queue.Queue()
def reader():
 for line in p.stdout:
  try:q.put(json.loads(line))
  except ValueError:pass
threading.Thread(target=reader,daemon=True).start()
def send(obj):p.stdin.write(json.dumps(obj)+'\n');p.stdin.flush()
def rpc(i,method,params):
 send({'jsonrpc':'2.0','id':i,'method':method,'params':params});end=time.time()+90
 while time.time()<end:
  try:o=q.get(timeout=1)
  except queue.Empty:continue
  if o.get('id')==i:return o
 raise TimeoutError(method)
try:
 rpc(1,'initialize',{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'foley-validation','version':'1'}})
 send({'jsonrpc':'2.0','method':'notifications/initialized'})
 if sys.argv[1]=='tools':
  o=rpc(2,'tools/list',{});(out/'bridge-tools.json').write_text(json.dumps(o),encoding='utf-8')
  for t in o['result']['tools']:
   if t['name'] in ['unity_editor','exec_editor_script']:print(json.dumps(t))
 else:
  request=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));o=rpc(2,'tools/call',request)
  (out/(Path(sys.argv[1]).stem+'-result.json')).write_text(json.dumps(o),encoding='utf-8');print(json.dumps(o))
finally:p.terminate()
