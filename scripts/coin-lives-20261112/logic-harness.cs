using System;using System.Collections.Generic;
static class PlayerPrefs {static Dictionary<string,int> data=new Dictionary<string,int>();public static int GetInt(string k,int d=0)=>data.ContainsKey(k)?data[k]:d;public static void SetInt(string k,int v){data[k]=v;}public static void DeleteKey(string k){data.Remove(k);}public static void Save(){}}
static class Mathf {public static int Clamp(int x,int a,int b)=>Math.Min(b,Math.Max(a,x));}
static class GameMenuUI {public static string PreferencePrefix="isolated.";}
static class GameplayAudio {public static void Play(string s){}}
class Scene {public string name="test";}class Go {public Scene scene=new Scene();}
class CampaignLevel {public Go gameObject=new Go();public bool Won;
string LivesKey => GameMenuUI.PreferencePrefix+"lives.v1."+gameObject.scene.name;
public int Lives => Mathf.Clamp(PlayerPrefs.GetInt(LivesKey,3),0,3);
public void LoseLife() {
        if(Won || Lives<=0)return;
        int remaining=Lives-1;PlayerPrefs.SetInt(LivesKey,remaining);
        if(remaining==0)for(int i=0;i<CoinTotal;i++)PlayerPrefs.DeleteKey(CoinKey(gameObject.scene.name,i));
        PlayerPrefs.Save();
    }
public bool CollectLegacyCoin() {
        for(int i=0;i<CoinTotal;i++)if(!HasCoin(i))return CollectCoin(i);
        return false;
    }
public bool HasCoin(int id) => PlayerPrefs.GetInt(CoinKey(gameObject.scene.name,id),0)==1;
public const int CoinTotal = 40;
static string CoinKey(string scene, int id) => GameMenuUI.PreferencePrefix+"collect.v1."+scene+"."+id;
public static int SavedCoins(string scene) { int count=0; for(int i=0;i<CoinTotal;i++) count+=PlayerPrefs.GetInt(CoinKey(scene,i),0)==1?1:0; return count; }
public int CoinCount => SavedCoins(gameObject.scene.name);
public static int CoinStars(int count) => count>=CoinTotal?3:count>=24?2:count>=10?1:0;
public bool CollectCoin(int id) {
        if(Won || Lives<=0 || id<0 || id>=CoinTotal || PlayerPrefs.GetInt(CoinKey(gameObject.scene.name,id),0)==1)return false;
        PlayerPrefs.SetInt(CoinKey(gameObject.scene.name,id),1);PlayerPrefs.Save();GameplayAudio.Play("coin");return true;
    }
public void Respawn(){if(Lives<=0){PlayerPrefs.SetInt(LivesKey,3);PlayerPrefs.Save();}}
}
class Test {static int n;static void Check(bool ok,string s){if(!ok)throw new Exception(s);Console.WriteLine("PASS "+s);n++;}static void Main(){
var a=new CampaignLevel();var b=new CampaignLevel();b.gameObject.scene.name="other";b.CollectCoin(0);
Check(a.Lives==3,"default 3 lives");Check(a.CollectCoin(4)&&a.CoinCount==1,"HUD data increments");Check(!a.CollectCoin(4)&&a.CoinCount==1,"duplicate ignored");Check(a.CollectLegacyCoin()&&a.CoinCount==2,"legacy pickup increments same count");
a.LoseLife();Check(a.Lives==2&&a.CoinCount==2,"first death retains coins");a.LoseLife();Check(a.Lives==1&&a.CoinCount==2,"second death retains coins");a.LoseLife();Check(a.Lives==0&&a.CoinCount==0,"third death clears coins");a.LoseLife();Check(a.Lives==0,"cannot become negative");Check(!a.CollectCoin(1),"zero lives blocks pickup");Check(b.CoinCount==1&&b.Lives==3,"other level preserved");a.Respawn();Check(a.Lives==3&&a.CoinCount==0,"respawn 3 lives zero coins");Check(a.CollectCoin(4),"coin recollectable");Check(CampaignLevel.CoinStars(a.CoinCount)==0,"stars follow reset count");Console.WriteLine(n+" checks passed; extracted current source, in-memory prefs, not Unity Play");}}
