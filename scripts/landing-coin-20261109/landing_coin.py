from pathlib import Path
import json,hashlib,shutil
r=Path(r'E:\TT Gaming\TT-Gaming-Getting-over-with-mario');out=r/'scripts/landing-coin-20261109';out.mkdir(exist_ok=True)
assert not (out/'evidence.json').exists()
changed=[]
def edit(rel,fn):
 p=r/rel;b=out/'backup'/rel;b.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,b);s=p.read_text(encoding='utf-8-sig');t=fn(s);assert t!=s;p.write_text(t,encoding='utf-8');changed.append(p)
def contact(s):
 s=s.replace('        float nextImpact;','        float nextImpact;\n        Rigidbody2D physicsBody;\n        Vector2 beforePhysicsVelocity;')
 s=s.replace('lifecycle = FindObjectOfType<Death>();','lifecycle = GetComponent<Death>();\n            if(lifecycle == null) lifecycle = FindObjectOfType<Death>();')
 s=s.replace('body = GetComponent<Death>() != null;','body = GetComponent<Death>() != null;\n            physicsBody = GetComponent<Rigidbody2D>();')
 s=s.replace('        public static float ImpactGain', '        void FixedUpdate()\n        {\n            if(physicsBody != null) beforePhysicsVelocity = physicsBody.velocity;\n        }\n        public static float ImpactGain')
 a=s.index('        void OnCollisionEnter2D(')
 s=s[:a]+'''        void OnCollisionEnter2D(Collision2D collision)
        {
            // Moving/falling platforms may use Default rather than the terrain layer.
            if(!body && collision.gameObject.layer != LayerMask.NameToLayer("Collider")) return;
            if(collision.transform.root == transform.root) return;
            float speed = 0;
            bool landing = false;
            Vector2 otherVelocity = collision.rigidbody == null ? Vector2.zero : collision.rigidbody.velocity;
            foreach(var contact in collision.contacts)
            {
                speed = Mathf.Max(speed, Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal)));
                if(body && contact.normal.y > .5f && beforePhysicsVelocity.y < -BodyThreshold)
                {
                    landing = true;
                    // The solver may already have stopped a fast-falling body by callback time.
                    speed = Mathf.Max(speed, Mathf.Max(0,-Vector2.Dot(beforePhysicsVelocity-otherVelocity,contact.normal)));
                }
            }
            // A genuine hard landing must not be swallowed by a recent wall-bump cooldown.
            if(landing && speed >= 8f) nextImpact = Mathf.Min(nextImpact, Time.unscaledTime);
            TryPlayImpact(speed);
        }
    }
}
'''
 return s
edit('Assets/Scripts/Character/ContactSoundEffect.cs',contact)
edit('Assets/Scripts/Winning/FloatingCoin.cs',lambda s:s.replace('GameplayAudio.Play("coin", .25f);','GameplayAudio.Play("coin", .08f);'))
edit('Assets/Scripts/GameplayAudio.cs',lambda s:s.replace('        Play(clip,volume * .45f,1f);','''        // Pickup feedback sits behind physical impacts in the mix.
        if(cue == "coin") volume = Mathf.Min(volume, .08f);
        if(cue == "star") volume = Mathf.Min(volume, .18f);
        if(cue == "stars_complete") volume = Mathf.Min(volume, .22f);
        Play(clip,volume * .45f,1f);'''))
(out/'evidence.json').write_text(json.dumps({'scope':'High-fall body landings and quieter pickups','checks':{'compile':'PENDING','physical_landing':'PENDING'},'sha256':{str(p.relative_to(r)):hashlib.sha256(p.read_bytes()).hexdigest() for p in changed}},indent=2),encoding='utf-8')
shutil.copy2(__file__,out/'landing_coin.py')
with (r/'ai-handoff.md').open('a',encoding='utf-8') as f:f.write('\n## High landings / quieter pickups 2026-11-09\nBody impact uses cached pre-physics approach velocity for upward ground contacts, supports Default-layer platforms, hard landing>=8 bypasses prior bump cooldown. Five grass recordings retained; gain capped and light hammer threshold unchanged. Coin .25->.08 (~32% amplitude); campaign pickup star/last-star capped .18/.22 instead of .65. No physical tuning, scene saves, gift or death-state changes. Evidence/backup scripts/landing-coin-20261109; engine/physical checks pending.\n')
print(out)
