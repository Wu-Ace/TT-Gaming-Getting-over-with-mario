const fs=require('fs'),path=require('path');const root=path.resolve(__dirname,'../..');
const locales=fs.readdirSync(path.join(root,'Assets/Resources/Localization')).filter(f=>f.endsWith('.json')).map(f=>JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/Localization',f),'utf8').replace(/^\uFEFF/,'')));
const source=locales.find(l=>l.code==='en'),zh=locales.find(l=>l.code==='zh-Hans');
const index=JSON.parse(fs.readFileSync(path.join(__dirname,'reference-index.json')));
const minecraft=JSON.parse(fs.readFileSync(path.join(__dirname,'minecraft-terms.json'))).results;
const keys={'menu.start':['Start','play','menu.singleplayer'],'settings.title':['Options','settings','menu.options'],'menu.exit':['Quit','quit','menu.quit'],'menu.back':['Back','back','gui.back'],'settings.music':['Music','setting.musicvol.name','options.sounds'],'settings.sfx':['Sound Effects','setting.sfxvol.name','options.sounds'],'settings.language':['Language','language','options.language'],'settings.resolution':['Resolution','resolution','options.fullscreen.resolution']};
function po(text,key){let re=new RegExp('^msgid '+JSON.stringify(key).replace(/[.*+?^${}()|[\]\\]/g,'\\$&')+'\\r?\\nmsgstr (".*")','m');let m=text.match(re);return m?JSON.parse(m[1]):'';}
let comparison=[];for(const locale of locales){let refs=index.filter(r=>r.code===locale.code&&r.status==='downloaded');let mine=minecraft.find(r=>r.code===locale.code&&r.status==='downloaded');let terms=[];
for(const [key,refkeys] of Object.entries(keys)){let examples=[];for(const r of refs){let file=path.join(__dirname,'references',r.game+'-'+r.url.split('/').at(-1));let text=fs.readFileSync(file,'utf8');let term=r.game==='SuperTuxKart'?po(text,refkeys[0]):(text.match(new RegExp('^'+refkeys[1].replaceAll('.','\\.')+'\\s*=\\s*(.+)$','m'))||[])[1];if(term)examples.push({game:r.game,text:term,source:r.source,contextWarning:key==='menu.start'?'Start/play contexts differ; do not blindly copy':''});}
if(mine&&mine.terms[refkeys[2]])examples.push({game:'Minecraft 1.21.1',text:mine.terms[refkeys[2]],source:mine.url,contextWarning:['settings.music','settings.sfx','menu.start'].includes(key)?'Different scope: audio category or single-player entry; terminology evidence only':''});
terms.push({key,english:source.entries.find(e=>e.key===key).text,chinese:zh.entries.find(e=>e.key===key).text,current:locale.entries.find(e=>e.key===key).text,examples});}
comparison.push({code:locale.code,terms,referenceStatus:locale.code==='en'?'source locale':refs.length||mine?'released-game terminology available':'NO RELEASED-GAME LANGUAGE REFERENCE FOUND',nativeSignOff:'NOT PERFORMED'});}
fs.writeFileSync(path.join(__dirname,'terminology-comparison.json'),JSON.stringify(comparison,null,2));
let md='# 已发布游戏术语对照\n\n发布版本：Mindustry v146、SuperTuxKart 1.4、Minecraft Java 1.21.1。只摘录少量 UI 短词；音频分类与主菜单动作需要区分上下文，不能机械照搬。全部语言仍待母语审校签收。\n';
for(const c of comparison){md+='\n## '+c.code+'\n\n'+c.referenceStatus+'\n\n|项目|当前|游戏参考|\n|---|---|---|\n';for(const t of c.terms)md+='|'+t.key+'|'+t.current+'|'+t.examples.map(e=>'['+e.game+']('+e.source+')：'+e.text).join('；')+'|\n';}
fs.writeFileSync(path.join(__dirname,'术语对照.md'),md);
let csv=['locale,key,english,chinese,current,review'];function quote(s){return '"'+s.replaceAll('"','""')+'"';}
for(const l of locales)for(const e of l.entries)csv.push([l.code,e.key,source.entries.find(x=>x.key===e.key).text,zh.entries.find(x=>x.key===e.key).text,e.text,l.code==='my'?'meaning reviewed; no released-game Burmese reference; native review required':'meaning reviewed; terminology references available; native sign-off pending'].map(quote).join(','));
fs.writeFileSync(path.join(__dirname,'all-strings-review.csv'),'\uFEFF'+csv.join('\n'));
console.log(JSON.stringify(comparison.map(c=>({code:c.code,referenceStatus:c.referenceStatus,examples:c.terms.reduce((n,t)=>n+t.examples.length,0)}))));
