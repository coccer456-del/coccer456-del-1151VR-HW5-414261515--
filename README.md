# coccer456-del-1151VR-HW5-414261515--

<img width="869" height="479" alt="image" src="https://github.com/user-attachments/assets/ad4d9682-9490-4fdd-87c8-2ae77419742f" />

https://github.com/coccer456-del/coccer456-del-1151VR-HW5-414261515--/blob/main/README.md

https://youtu.be/-uzWca3YmHQ


主要為四大部分

1.放入訓練家、寶可夢模型、建立草地、樹木、山景	 

  基本上素材部分都由AI生成，我試過自己畫的山實在太醜了

2.精靈球拋物線飛行(次難)

  這邊最重要的就是這段
  
  ball.position = Vector3.Lerp(start, hit, eased) + Vector3.up * (4f * arcHeight * t * (1f - t));
  
  精靈球位置=直線移動位置+額外高度(x,t)座標
  
  值得注意的是公式h=4Ht(1-t)
  
  h：現在要增加的高度
  
  H：arcHeight，設定最高額外高度
  
  t：飛行進度，從 0 增加到 1
		
3.收服動畫與特效同步

  特效也很多但我覺得比較難的是收服精靈後使精靈球晃動三次且幅度越來越小
  
  for (int i = 0; i < 3; i++){
    float wobble = Mathf.Sin(t * Mathf.PI * 2f) * (22f - i * 4f);
    //利用公式sin(2*pi*t)設定晃動為週期性變化
    //乘上(22f - i * 4f)使的幅度隨次數變小
    ball.rotation = ballStartRotation * Quaternion.Euler(0f, 0f, wobble);
  }
  
  老實說我真沒想到用sin原本想一步步調整



4.右手的拋球動作(最難)
  
  我的作法是將手跟身體做成父子兩物件
  
  GameObject armPivotObject = new GameObject("RIGHT SHOULDER PIVOT");
  
  Transform armPivot = armPivotObject.transform;
  
  armPivot.SetParent(trainer.transform, false);
  
  //設定RIGHT SHOULDER PIVOT為子物件
  
  armPivot.localPosition = new Vector3(-0.59f, 1.88f, -0.04f);	
  
  最後用throwArmPivot.localRotation = Quaternion.Slerp(from, target, t);
  
  //來執行丟球時的角度及時間
  
  球如何跟著手臂一啟動呢?
  
  GameObject throwOrigin = new GameObject("THROW POINT - In Right Hand");
  
  //設定球為右手之子物件
  
  throwOrigin.transform.SetParent(armPivot, false);//球根哲手臂憶起轉動
  
  throwOrigin.transform.localPosition = new Vector3(-0.10f, -0.92f, -0.31f);//設定球初始位置
  
  throwOrigin.transform.localRotation = Quaternion.identity;
  
  //設定出球點的初始方向，不額外旋轉
  
接下來第227行開始設定拿球舉手動畫(就不詳細說明主要難在規劃父子物件)
