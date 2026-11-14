using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Shared campaign scope; menus and tutorial retain their authored presentation.
public static class CampaignAtmosphere
{
    public static bool IsLevel(string name) => name == "Level01" || name == "Level02_Jungle" || name == "Level03_Beach" || name == "Level04_Town" || name == "Level05_Cave";
}

public sealed class CampaignExtraEnemies : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }
    static void Loaded(Scene scene, LoadSceneMode mode) {
        if (!CampaignAtmosphere.IsLevel(scene.name) || scene.name == "Level01") return;
        foreach(var root in scene.GetRootGameObjects()) if(root.GetComponent<CampaignExtraEnemies>()) return;
        var go = new GameObject("Additional platform patrols"); SceneManager.MoveGameObjectToScene(go, scene); go.AddComponent<CampaignExtraEnemies>();
    }
    void Start() {
        var level = FindObjectOfType<CampaignLevel>();
        var occupied = new List<Vector2>();
        foreach(var e in FindObjectsOfType<Enemy.EnemyMovement>()) occupied.Add(e.transform.position);
        int added = 0;
        foreach(var map in FindObjectsOfType<Tilemap>()) {
            if(map.gameObject.scene != gameObject.scene || !map.GetComponent<TilemapCollider2D>()) continue;
            var bounds = map.cellBounds;
            for(int y=bounds.yMin; y<bounds.yMax && added<16; y++) {
                int start=bounds.xMin;
                while(start<bounds.xMax && added<16) {
                    while(start<bounds.xMax && !Surface(map,start,y)) start++;
                    int end=start; while(end<bounds.xMax && Surface(map,end,y)) end++;
                    if(end-start>=7) {
                        Vector2 p=map.GetCellCenterWorld(new Vector3Int((start+end-1)/2,y,0));
                        p.y=map.CellToWorld(new Vector3Int(start,y+1,0)).y+.2f;
                        bool clear=level && Vector2.Distance(p,level.initialSpawn)>8;
                        foreach(var old in occupied) if(Vector2.Distance(old,p)<7) clear=false;
                        if(clear) {
                            var prefab=Resources.Load<GameObject>(added%3==2 ? "CampaignEnemies/Pig" : "CampaignEnemies/Sheep");
                            if(prefab) { var enemy=Instantiate(prefab,p,Quaternion.identity,transform); enemy.name="Platform patrol "+(++added); occupied.Add(p); }
                        }
                    }
                    start=end+1;
                }
            }
        }
    }
    static bool Surface(Tilemap map,int x,int y) {
        if(!map.HasTile(new Vector3Int(x,y,0))) return false;
        for(int h=1;h<=5;h++) if(map.HasTile(new Vector3Int(x,y+h,0))) return false;
        return true;
    }
}
