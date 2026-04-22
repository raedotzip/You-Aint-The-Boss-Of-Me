using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2AnimTest : MonoBehaviour
{
    private Animator animator;
    private int timer = 0;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < 2000)
        {
            if (timer % 200 == 0)
            {
                animator.SetTrigger("Hurt");
            }
            timer++;

        } else
        {
            animator.SetBool("Destroyed", true);
        }
    }
}
