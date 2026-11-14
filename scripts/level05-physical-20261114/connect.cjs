const {spawn}=require('child_process');
const p=spawn('E:/Tuanjie Cowork/cli/bin/win32-x64/codely.exe',['serve','unity-mcp','--stdio','--unity-project-path','.'],{shell:false});let buf='';
p.stderr.on('data',d=>process.stderr.write(d));p.stdout.on('data',d=>{buf+=d;let i;while((i=buf.indexOf('\n'))>=0){let l=buf.slice(0,i);buf=buf.slice(i+1);try{let r=JSON.parse(l);if(r.id===1)p.stdin.write(JSON.stringify({jsonrpc:'2.0',id:2,method:'tools/call',params:JSON.parse(require('fs').readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''))})+'\n');if(r.id===2){console.log(JSON.stringify(r));p.kill();process.exit(0)}}catch{}}});
p.stdin.write(JSON.stringify({jsonrpc:'2.0',id:1,method:'initialize',params:{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'Codex',version:'1'}}})+'\n');setTimeout(()=>{p.kill();process.exit(2)},180000);

