using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode.Components;

namespace BaldiEnemy;

public class BaldiEnemy : EnemyAI
{
    [SerializeField] AudioClip slap;
    [SerializeField] AudioClip buzz;
    [SerializeField] NetworkAnimator networkAnimator;
    [SerializeField] GameObject billboard;
    [SerializeField] GameObject pivot;
    float moveTimer;
    enum States
    {
        Roam,
        Active
    }
    public override void Start()
    {
        base.Start();
        agent.speed = 0;
        moveTimer = 0;
        BaldiHearingManager.RegisterSpawnedBaldi(this);
    }
    public override void Update()
    {
        base.Update();
        if (targetPlayer != null && targetPlayer.isPlayerDead) targetPlayer = null;
        moveTimer += Time.deltaTime;
        if (moveTimer >= 2.2f)
        {
            StartCoroutine(DoMovement());
            creatureSFX.PlayOneShot(slap);
            creatureAnimator.SetTrigger("slap");
            moveTimer = 0;
        }
        var cameraPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position;
        var direction = transform.position - cameraPos;
        direction.y = 0;
        var targetRot = Quaternion.LookRotation(direction);
        pivot.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, targetRot.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
    IEnumerator DoMovement()
    {
        agent.speed = 100;
        yield return new WaitForSeconds(0.1f);
        agent.speed = 0;
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        switch (currentBehaviourStateIndex)
        {
            case (int)States.Roam:
                Roam();
                break;
            case (int)States.Active:
                Active();
                break;
        }
    }
    public void Roam()
    {
        var colliders = Physics.OverlapSphere(transform.position, 50, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);
        foreach (Collider c in colliders)
        {
            if (c.gameObject.TryGetComponent(out PlayerControllerB player) && player.isPlayerControlled && !player.isPlayerDead)
            {
                targetPlayer = player;
                SwitchToBehaviourClientRpc((int)States.Active);
            }
        }
    }

    public void HearDoorStateChange(Vector3 DoorPosition)
    {
        if (currentBehaviourStateIndex == (int)States.Roam)
        {
            SetDestinationToPosition(DoorPosition);
        }
    }

    public void Active()
    {
        if (targetPlayer == null || Vector3.Distance(targetPlayer.transform.position, transform.position) > 100f)
        {
            SwitchToBehaviourClientRpc((int)States.Roam);
            return;
        }
        SetDestinationToPosition(targetPlayer.transform.position);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null) return;
        player.DamagePlayer(999);
        creatureVoice.PlayOneShot(buzz);
    }

    public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false)
    {
        int layer = enable ? 19 : 23;
        if (!billboard.CompareTag("DoNotSet") || overrideDoNotSet) billboard.layer = layer;
    }
}
