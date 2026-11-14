const {spawn}=require('child_process');
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../..');
const child=spawn('E:/Tuanjie Cowork/cli/bin/win32-x64/codely.exe',['serve','unity-mcp','--stdio','--unity-project-path',root],{stdio:['pipe','pipe','ignore'],windowsHide:true});
let buffer='',id=0;const pending=new Map();
child.stdout.on('data',chunk=>{buffer+=chunk.toString('utf8');let index;while((index=buffer.indexOf('\n'))>=0){const line=buffer.slice(0,index);buffer=buffer.slice(index+1);try{const msg=JSON.parse(line);if(pending.has(msg.id)){pending.get(msg.id)(msg);pending.delete(msg.id);}}catch{}}});
function rpc(method,params){return new Promise((resolve,reject)=>{const n=++id;const timer=setTimeout(()=>{pending.delete(n);reject(new Error('Bridge timeout: '+method));},60000);pending.set(n,m=>{clearTimeout(timer);resolve(m)});child.stdin.write(JSON.stringify({jsonrpc:'2.0',id:n,method,params})+'\n');});}
(async()=>{try{
 await rpc('initialize',{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'localization-validation',version:'1'}});
 child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'})+'\n');
 const input=process.argv[2];const result=input==='tools'?await rpc('tools/list',{}):await rpc('tools/call',JSON.parse(fs.readFileSync(input,'utf8').replace(/^\uFEFF/,'')));
 const dest=path.join(__dirname,input==='tools'?'bridge-tools.json':path.basename(input).replace(/\.json$/,'')+'-result.json');
 fs.writeFileSync(dest,JSON.stringify(result,null,2));
 if(input==='tools')console.log(JSON.stringify(result.result.tools.filter(t=>/editor|script|job|console/.test(t.name))));else console.log(JSON.stringify(result));
 }finally{child.kill();}})().catch(e=>{console.error(e.message);process.exitCode=1;});
