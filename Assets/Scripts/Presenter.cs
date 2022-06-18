using System;
using UnityEngine;

public class Presenter : MonoBehaviour
{
    public CustomButton btn1;
    public CustomButton btn2;
    public CustomButton btn3;
    private void Start()
    {
        btn1.onClickCallback = () => { Debug.Log("タップした時の処理1"); };
        btn2.onClickCallback = () => { Debug.Log("タップした時の処理2"); };
        btn3.onClickCallback = () => { Debug.Log("タップした時の処理3"); };
    }
}