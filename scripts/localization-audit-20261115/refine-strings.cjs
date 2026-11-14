const fs=require('fs'),path=require('path');const root=path.resolve(__dirname,'../..');
const changes=[
{code:'es',key:'settings.sfx',text:'Efectos de sonido',reason:'Efectos alone can mean visual effects; explicitly retain the Chinese 音效 / English Sound FX meaning.',reference:'SuperTuxKart 1.4 Spanish/Minecraft audio category context; retained sound-specific wording'},
{code:'ar',key:'settings.sfx',text:'المؤثرات الصوتية',reason:'Specify sound effects rather than effects in general.',reference:'SuperTuxKart 1.4 ar.po: Sound Effects'},
{code:'my',key:'settings.resolution',text:'ကြည်လင်ပြတ်သားမှု',reason:'Previous ရုပ်ထွက်အရည်အသွေး means picture quality; this control selects pixel resolution.',reference:'https://www.nara.accu.or.jp/el/textpdf/Basic_Knowledge_of_Cultural_Properties_Photograph_%5BWorksheet%5D.pdf; technical Burmese reference, NOT a released-game/native sign-off'}
];
let records=[];for(const change of changes){const file=path.join(root,'Assets/Resources/Localization',change.code+'.json');const table=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));const entry=table.entries.find(e=>e.key===change.key);records.push({...change,before:entry.text});entry.text=change.text;fs.writeFileSync(file,JSON.stringify(table,null,2)+'\n');}
const generator=fs.readFileSync(path.join(root,'scripts/localization-20261114/build-tables.ps1'),'utf8');const keys=[...generator.matchAll(/'((?:menu|settings|level|result|hud|loading|error)\.[^']+)'/g)].map(m=>m[1]).slice(0,21);
let lines=fs.readFileSync(path.join(root,'scripts/localization-20261114/translations.tsv'),'utf8').replace(/^\uFEFF/,'').trimEnd().split(/\r?\n/);
for(const change of changes){let i=lines.findIndex(l=>l.startsWith(change.code+'|')),cells=lines[i].split('|'),index=keys.indexOf(change.key);if(index<0)throw Error(change.key);cells[index+3]=change.text;lines[i]=cells.join('|');}
fs.writeFileSync(path.join(root,'scripts/localization-20261114/translations.tsv'),lines.join('\n')+'\n');fs.writeFileSync(path.join(__dirname,'translation-changes.json'),JSON.stringify(records,null,2));console.log(JSON.stringify(records));
