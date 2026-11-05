from pathlib import Path
p=Path('Assets/Scripts/GameMenuUI.cs');s=p.read_text();s=s.replace('            GUI.DrawTexture(new Rect(285,49,40,40),star,ScaleMode.ScaleToFit);','''            // HUD anchors follow the actual display, independently of the menu reference frame.
            float hudScale=Mathf.Clamp(Screen.height/900f,.65f,2f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(hudScale,hudScale,1));
            GUI.DrawTexture(new Rect(40,75,32,32),star,ScaleMode.ScaleToFit);''').replace('new Rect(330,42,150,55)','new Rect(80,65,150,55)').replace('new Rect(1380,40,160,68)','new Rect(Screen.width/hudScale-180,25,160,60)');s=s.replace('        if(settings)Settings();','        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));\n        if(settings)Settings();');p.write_text(s,encoding='utf8')
p=Path('Assets/Editor/MenuUIAcceptance.cs');s=p.read_text(encoding='utf-8-sig').replace('Require(Camera.main==null || true,"Editor Play entered");','');p.write_text(s,encoding='utf8')
