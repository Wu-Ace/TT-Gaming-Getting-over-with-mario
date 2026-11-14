import subprocess,json,threading,queue
q=queue.Queue()
p=subprocess.Popen(['codely','serve','unity-mcp','--stdio','--unity-project-path','.'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True,encoding='utf-8')
threading.Thread(target=lambda:[q.put(line) for line in p.stdout],daemon=True).start()
def rpc(i,method,params):
 p.stdin.write(json.dumps(dict(jsonrpc='2.0',id=i,method=method,params=params))+'\n');p.stdin.flush()
 while True:
  line=q.get(timeout=55)
  try:r=json.loads(line)
  except:continue
  if r.get('id')==i:return r
try:
 import sys
 rpc(1,'initialize',dict(protocolVersion='2024-11-05',capabilities={},clientInfo=dict(name='Codex',version='1')))
 request=json.load(open(sys.argv[1],encoding='utf-8-sig'))
 result=rpc(2,'tools/call',request)
 print(json.dumps(result,ensure_ascii=False))
finally:p.terminate()
