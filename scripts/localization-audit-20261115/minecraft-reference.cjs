const fs=require('fs'),path=require('path');
const root=__dirname;
const mapping={'ar':'ar_sa','az':'az_az','bg':'bg_bg','de':'de_de','el':'el_gr','en':'en_us','es':'es_es','fr':'fr_fr','he':'he_il','hr':'hr_hr','hu':'hu_hu','id':'id_id','it':'it_it','ja':'ja_jp','ko':'ko_kr','ms':'ms_my','my':'my_mm','nl':'nl_nl','pl':'pl_pl','pt-BR':'pt_br','ro':'ro_ro','ru':'ru_ru','sv':'sv_se','th':'th_th','tr':'tr_tr','uk':'uk_ua','vi':'vi_vn','zh-Hans':'zh_cn','zh-Hant':'zh_tw'};
async function get(url){for(let n=0;n<3;n++)try{let r=await fetch(url,{signal:AbortSignal.timeout(25000)});if(!r.ok)throw Error(r.status);return await r.json();}catch(e){if(n===2)throw e;}}
(async()=>{
const manifest=await get('https://piston-meta.mojang.com/mc/game/version_manifest_v2.json');
const version=manifest.versions.find(v=>v.id==='1.21.1');const detail=await get(version.url);const index=await get(detail.assetIndex.url);
let tasks=Object.entries(mapping),results=[];
await Promise.all(Array.from({length:5},async()=>{while(tasks.length){const [code,locale]=tasks.shift();const item=index.objects['minecraft/lang/'+locale+'.json'];if(!item){results.push({code,locale,status:'not in external language assets'});continue;}
const url=`https://resources.download.minecraft.net/${item.hash.slice(0,2)}/${item.hash}`;
try{const table=await get(url);const keys=['menu.options','menu.singleplayer','menu.quit','gui.back','gui.done','options.sounds','options.music','options.sound','options.language','options.fullscreen.resolution','options.auto','selectWorld.select','menu.returnToMenu'];const terms={};for(const key of keys)if(table[key])terms[key]=table[key];results.push({code,locale,version:'1.21.1',status:'downloaded',url,hash:item.hash,terms});}catch(e){results.push({code,locale,status:'fetch failed',error:e.message});}}}));
fs.writeFileSync(path.join(root,'minecraft-terms.json'),JSON.stringify({versionMetadata:version.url,assetIndex:detail.assetIndex.url,results},null,2));console.log(JSON.stringify(results.map(r=>({code:r.code,status:r.status,terms:r.terms}))));
})().catch(e=>{console.error(e);process.exitCode=1});
