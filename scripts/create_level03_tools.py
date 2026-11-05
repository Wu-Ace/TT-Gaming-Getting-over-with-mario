from pathlib import Path
s=Path('Assets/Editor/Level02HorizontalBuilder.cs').read_text(encoding='utf-8-sig')
s=s.replace('Level02HorizontalBuilder','Level03DesignedBuilder').replace('level02-horizontal','level03-designed').replace('Level02_Jungle','Level03_Beach').replace('L02','L03')
s=s.replace('Directory.CreateDirectory("art-backup/pre-level03-designed");','Directory.CreateDirectory("art-backup/pre-level03-designed");')
s=s.replace('Field("theme",level.theme);','level.initialSpawn=new Vector2(2,-2.9f);Field("theme",level.theme);')
s=s.replace('for(int i=0;i<3;i++)Call("Star",i,plan.stars[i]);','''for(int i=0;i<3;i++)Call("Star",i,plan.stars[i]);
                foreach(var gem in objects.GetComponentsInChildren<CampaignInteractable>().Where(i=>i.kind==CampaignInteractable.Kind.Star)){gem.name="Objective Diamond "+(gem.starId+1);gem.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/diamond.png");}
                foreach(var r in terrain.GetComponentsInChildren<SpriteRenderer>()){
                    var original=r.sprite;
                    r.sprite=(Sprite)typeof(KitTerrainRepair).GetMethod("SeamSafeSprite",Flags).Invoke(null,new object[]{original});
                    r.transform.localScale=Vector3.one*5;
                }''')
s=s.replace('level.route=plan.route;level.title=plan.title;','''foreach(var text in objects.GetComponentsInChildren<TextMesh>())text.text=text.text.Replace("STAR","DIAMOND");
                level.route=plan.route;level.title=plan.title;''')
s=s.replace('new Vector3(42,1,0)','new Vector3(12,0,0)')
s=s.replace('new Vector3(152,1,-20)','new Vector3(130,3,-20)').replace('c.orthographicSize=15','c.orthographicSize=18')
s=s.replace('File.Delete("scripts/level03-designed.request");','''if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){Debug.LogWarning("Save current scene before Level03 build");return;}
        File.Delete("scripts/level03-designed.request");''')
Path('Assets/Editor/Level03DesignedBuilder.cs').write_text(s)
# Reuse established real Play regression for one scene, preserving isolated save prefix.
s=Path('Assets/Editor/FirstTwoAcceptance.cs').read_text(encoding='utf-8-sig').replace('FirstTwoAcceptance','ThirdLevelAcceptance').replace('first-two-test','third-level-test').replace('first-two-evidence','level03-designed-evidence')
s=s.replace('Assets/Scenes/OneOneScene.unity','Assets/Scenes/Level03_Beach.unity')
s=s.replace('(s.path.EndsWith("/Level02_Jungle.unity") || s.path.EndsWith("/OneOneScene.unity"))','s.path.EndsWith("/Level03_Beach.unity")').replace('level.number==levelIndex+1','level.number==3')
Path('Assets/Editor/ThirdLevelAcceptance.cs').write_text(s)
Path('scripts/level03-designed.request').write_text('')
