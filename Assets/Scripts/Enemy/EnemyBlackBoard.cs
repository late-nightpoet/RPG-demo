using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyBlackBoard
{
   //角色当前的真实移动速度（包含水平x/z轴和垂直y轴（jump和fall时会有垂直的速度））
    public Vector3 velocity;

    public CharacterController controller;
    public bool isGrounded = true;

    public float gravityMultiplier = 1f;
}
