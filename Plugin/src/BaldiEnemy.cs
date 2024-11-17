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
    float moveTime = 2.5f;
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
        WanderToRandomNode();
        //Plugin.Logger.Loginfo("A Baldi has registered for hearing manager");
    }
    public override void Update()
    {
        base.Update();

        //Force reset our target if we kill it
        //logic relies on this elsewhere
        if (targetPlayer != null && targetPlayer.isPlayerDead) targetPlayer = null;

        //increment movement timer each frame
        moveTimer += Time.deltaTime;

        if (moveTimer >= moveTime)
        {
            StartCoroutine(DoMovement());
            creatureSFX.PlayOneShot(slap);
            creatureAnimator.SetTrigger("slap");
            moveTimer = 0;

            float coeff = RoundManager.Instance.valueOfFoundScrapItems / RoundManager.Instance.totalScrapValueInLevel;

            moveTime = Mathf.Lerp(2.5f, 0.75f, coeff);

            if (RoundManager.Instance.powerOffPermanently) moveTime *= 0.75f; // >:3
        }

        //Always look towards the active camera
        var cameraPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position;
        var direction = transform.position - cameraPos;
        direction.y = 0;
        var targetRot = Quaternion.LookRotation(direction);
        pivot.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, targetRot.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
    IEnumerator DoMovement()
    {
        //Plugin.Logger.Loginfo($"Baldi is doing movement destination: {destination}");
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
        if (UpdateTargetSelection())
        {
            SwitchToBehaviourClientRpc((int)States.Active);
            //Plugin.Logger.Loginfo("Baldi is switching to Active");

            //This will prevent Baldi from running the rest of the
            //wandering monster code when he's switching to attack mode
            return;
        }


        //Only update our target position if we've reached our current target
        if (agent.remainingDistance < 1f)
        {
            WanderToRandomNode();
        }
    }

    protected bool WanderToRandomNode()
    {
        /*
                This code adapted from AntiSlimeCamp by TestAccount
                I had to modify it quite a bit because it was doing very
                strange things with a method called NextInt that just doesn't
                exist anywhere in any extensions or objects I checked >_<

                This code was greatly simplified from its original form
            */

        var array = isOutside ? RoundManager.Instance.outsideAINodes : RoundManager.Instance.insideAINodes;

        if (array is null)
        {
            Plugin.Logger.LogError($"Monster was unable to choose a node");
            return false;
        }

        int num = new System.Random().Next(0, array.Length);
        Plugin.Logger.LogMessage($"Monster has chosen node {num} to wander to");
        SetDestinationToPosition(array[num].transform.position);

        return true;
    }

    /// <summary>
    /// Tries to find a target for Baldi
    /// </summary>
    /// <returns>True if a target is found, false otherwise</returns>
    protected bool UpdateTargetSelection()
    {
        var closestTarget = this.GetClosestPlayer();

        if (closestTarget == null)
        {
            targetPlayer = null;
            return false;
        }

        if (Vector3.Distance(this.transform.position, closestTarget.transform.position) < 50f)
        {
            targetPlayer = closestTarget;
            return true;
        }

        targetPlayer = null;
        return false;
    }

    public void HearDoorStateChange(Vector3 DoorPosition)
    {
        if (currentBehaviourStateIndex == (int)States.Roam)
        {
            //Plugin.Logger.Loginfo($"Baldi has heard a door update at {DoorPosition}");
            SetDestinationToPosition(DoorPosition);
        }
    }

    public void Active()
    {

        if (UpdateTargetSelection())
        {
            SetDestinationToPosition(targetPlayer.transform.position);
        }
        else
        {
            //Seems goofy but we set our destination to ourself so our
            //remaining distance to target is zero in the check that comes
            //up later. This is kind of a "reset" of our target logic
            SetDestinationToPosition(transform.position);
            SwitchToBehaviourClientRpc((int)States.Roam);
        }
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
