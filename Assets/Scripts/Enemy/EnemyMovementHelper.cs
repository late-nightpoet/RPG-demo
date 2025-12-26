using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyMovementHelper
{
    private readonly EnemyBlackBoard enemyBlackBoard;

    public EnemyMovementHelper(EnemyBlackBoard enemyBlackBoard)
    {
        this.enemyBlackBoard = enemyBlackBoard;
    }

    public void ApplyGravity()
    {
        //ctx.velocity.y += Physics.gravity.y * ctx.gravityMultiplier * Time.deltaTime;
        if (enemyBlackBoard.velocity.y > Physics.gravity.y)
        {
            enemyBlackBoard.velocity.y += Physics.gravity.y * enemyBlackBoard.gravityMultiplier * Time.deltaTime;
            Debug.Log("enemyBlackBoard.velocity.y is " + enemyBlackBoard.velocity.y);
        }
    }

    public void GroundedCheck()
    {
        if (enemyBlackBoard.controller == null)
        {
            enemyBlackBoard.isGrounded = false;
            return;
        }

        enemyBlackBoard.isGrounded = enemyBlackBoard.controller.isGrounded;
        if (enemyBlackBoard.isGrounded && enemyBlackBoard.velocity.y < 0f)
        {
            enemyBlackBoard.velocity.y = 0f;
        }
    }

    public void Move()
    {
        if (enemyBlackBoard.controller == null)
        {
            return;
        }

        enemyBlackBoard.controller.Move(enemyBlackBoard.velocity * Time.deltaTime);
    }
}
