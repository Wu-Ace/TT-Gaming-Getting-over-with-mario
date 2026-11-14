const fs=require('fs'),path=require('path');
const dir=path.join(__dirname,'references');fs.mkdirSync(dir,{recursive:true});
const codes=['ar','az','bg','de','el','en','es','fr','he','hr','hu','id','it','ja','ko','ms','my','nl','pl','pt-BR','ro','ru','sv','th','tr','uk','vi','zh-Hans','zh-Hant'];
const refs=[{name:'Mindustry',repo:'Anuken/Mindustry',ref:'v146',folder:'core/assets/bundles',file:c=>'bundle'+(c==='en'?'':'_'+({'pt-BR':'pt_BR','zh-Hans':'zh_CN','zh-Hant':'zh_TW'}[c]||c))+'.properties'},
{name:'SuperTuxKart',repo:'supertuxkart/stk-code',ref:'1.4',folder:'data/po',file:c=>({'pt-BR':'pt_BR','zh-Hans':'zh_CN','zh-Hant':'zh_TW'}[c]||c)+'.po'}];
async function get(url){let r=await fetch(url,{signal:AbortSignal.timeout(25000),headers:{'User-Agent':'localization-review'}});if(!r.ok)throw Error(r.status+' '+url);return r.text();}
(async()=>{let report=[];for(const ref of refs){let listing;try{listing=JSON.parse(await get(`https://api.github.com/repos/${ref.repo}/contents/${ref.folder}?ref=${ref.ref}`));}catch(e){report.push({game:ref.name,error:e.message});continue;}
let tasks=codes.map(code=>async()=>{let item=listing.find(f=>f.name===ref.file(code));if(!item){report.push({game:ref.name,code,status:'unavailable in release'});return;}let url=`https://raw.githubusercontent.com/${ref.repo}/${ref.ref}/${ref.folder}/${item.name}`;try{let body=await get(url);fs.writeFileSync(path.join(dir,ref.name+'-'+item.name),body);report.push({game:ref.name,code,status:'downloaded',url,source:`https://github.com/${ref.repo}/blob/${ref.ref}/${ref.folder}/${item.name}`});}catch(e){report.push({game:ref.name,code,error:e.message});}});
await Promise.all(Array.from({length:6},async()=>{while(tasks.length)await tasks.shift()();}));}
fs.writeFileSync(path.join(__dirname,'reference-index.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report.map(x=>({game:x.game,code:x.code,status:x.status,error:x.error}))));})().catch(e=>{console.error(e);process.exitCode=1});
