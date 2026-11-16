using System.Collections;
using UnityEngine;

/// <summary>
/// 精靈球捕捉示範：雙手垂下，右手伸直舉球後才出球；R 重置。
/// 所有移動都由 Coroutine 控制，不依賴外部套件。
/// </summary>
public class CaptureDemoController : MonoBehaviour
{
    [Header("場景物件")]
    public GameObject trainer;
    public GameObject pokemon;
    public GameObject pokeBall;
    public Transform throwPoint;

    [Header("右手伸直舉球動畫（待機時雙手垂下）")]
    [Tooltip("右肩關節：身體只有一隻右手，不會產生重複手臂")]
    public Transform throwArmPivot;
    [Tooltip("按下空白鍵後，右臂從垂下姿勢舉直所需秒數")]
    [Min(0.05f)] public float armRaiseDuration = 0.45f;
    [Tooltip("右手舉直出球的角度。以角色面向 -Z 時的自身右手（Unity -X）計算")]
    [Range(70f, 160f)] public float releaseAngle = 115f;
    [Tooltip("手臂舉到定位後再出球，短暫停頓會更容易看到姿勢")]
    [Min(0f)] public float releaseHoldDuration = 0.06f;
    [Tooltip("出球後右手逐漸回到自然下垂的時間")]
    [Min(0.05f)] public float armReturnDuration = 0.60f;
    [Tooltip("尚未出手時，掌中精靈球大小（相對飛行時）")]
    [Range(0.35f, 1f)] public float heldBallScale = 0.72f;

    [Header("拋物線")]
    public float throwDuration = 1.15f;
    public float arcHeight = 3.3f;
    public float shrinkDuration = 0.70f;

    private Vector3 pokemonStartPosition;
    private Vector3 pokemonStartScale;
    private Quaternion pokemonStartRotation;
    private Vector3 ballStartScale;
    private Quaternion ballStartRotation;
    private Quaternion armRestRotation;
    private bool isBusy;
    private bool isCaught;
    private GUIStyle statusStyle;
    private GUIStyle hintStyle;
    private int lastFramePressed;

    private void Start()
    {
        if (pokemon == null || pokeBall == null || throwPoint == null)
        {
            Debug.LogError("CaptureDemoController: 請設定 Pokemon、Poke Ball、Throw Point。");
            enabled = false;
            return;
        }

        pokemonStartPosition = pokemon.transform.position;
        pokemonStartScale = pokemon.transform.localScale;
        pokemonStartRotation = pokemon.transform.rotation;
        ballStartScale = pokeBall.transform.localScale;
        ballStartRotation = pokeBall.transform.rotation;
        if (throwArmPivot != null) armRestRotation = throwArmPivot.localRotation;
        ResetScene();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines();
            ResetScene();
            return;
        }

        if (isBusy || isCaught) return;

        // 觸控也可用：Unity 將第一個觸控映射成滑鼠左鍵（預設設定）。
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) &&
            lastFramePressed != Time.frameCount)
        {
            lastFramePressed = Time.frameCount;
            StartCoroutine(CaptureSequence());
        }
    }

    private void ResetScene()
    {
        isBusy = false;
        isCaught = false;
        pokemon.SetActive(true);
        pokemon.transform.position = pokemonStartPosition;
        pokemon.transform.rotation = pokemonStartRotation;
        pokemon.transform.localScale = pokemonStartScale;
        if (throwArmPivot != null) throwArmPivot.localRotation = armRestRotation;
        // Before throwing: BOTH arms point down and the Poké Ball is in the right hand.
        pokeBall.transform.position = throwPoint.position;
        pokeBall.transform.rotation = ballStartRotation;
        pokeBall.transform.localScale = ballStartScale * heldBallScale;
        pokeBall.SetActive(true);
    }

    private IEnumerator CaptureSequence()
    {
        isBusy = true;
        Transform ball = pokeBall.transform;
        ball.position = throwPoint.position;
        ball.rotation = ballStartRotation;
        pokeBall.SetActive(true);

        // 0. 待機：左右手自然垂下。丟球：只有角色自身的右臂（-X）伸直往外舉起。
        // 右手只有 ONE 個模型（不是多生一隻手）；球跟隨右手手掌。
        Quaternion releaseRotation = armRestRotation;
        if (throwArmPivot != null)
        {
            // Character faces -Z: right shoulder is -X; NEGATIVE Z
            // rotation lifts the right hand outwards, never the left arm.
            releaseRotation = armRestRotation * Quaternion.Euler(0f, 0f, -releaseAngle);
            yield return RaiseStraightArmWithBall(releaseRotation, armRaiseDuration, ball);
        }

        // 伸直且舉到定位後，才鬆手讓球開始飛出。
        if (releaseHoldDuration > 0f) yield return new WaitForSeconds(releaseHoldDuration);
        Vector3 start = throwPoint.position;
        Vector3 hit = pokemonStartPosition + Vector3.up * 1.25f + Vector3.back * 0.2f;

        // 1. 精靈球由左向右拋出（含拋物線）。
        float timer = 0f;
        while (timer < throwDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / throwDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            ball.position = Vector3.Lerp(start, hit, eased)
                            + Vector3.up * (4f * arcHeight * t * (1f - t));
            ball.rotation = ballStartRotation * Quaternion.Euler(t * 540f, t * 240f, t * 720f);
            // 球剛離開右掌時順勢恢復成正常飛行大小。
            ball.localScale = Vector3.Lerp(ballStartScale * heldBallScale,
                                           ballStartScale, Mathf.Clamp01(t / 0.13f));

            // 球飛出去後，右臂回到身側；左臂從頭到尾不動。
            if (throwArmPivot != null)
            {
                float armT = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(timer / Mathf.Max(0.01f, armReturnDuration)));
                throwArmPivot.localRotation = Quaternion.Slerp(releaseRotation, armRestRotation, armT);
            }
            yield return null;
        }
        ball.position = hit;
        if (throwArmPivot != null) throwArmPivot.localRotation = armRestRotation;

        // 2. 命中：金色粒子、光圈，寶可夢縮小後收進球內。
        CaptureSparkleEffect.Burst(hit, new Color(1f, 0.86f, 0.24f), 44, 4.7f);
        CaptureSparkleEffect.Ring(hit, new Color(1f, 0.94f, 0.55f), 1.7f);
        CaptureSparkleEffect.Flash(hit, new Color(1f, 0.94f, 0.65f));
        timer = 0f;
        while (timer < shrinkDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / shrinkDuration));
            pokemon.transform.position = Vector3.Lerp(pokemonStartPosition, hit, t);
            pokemon.transform.localScale = Vector3.Lerp(pokemonStartScale, Vector3.zero, t);
            ball.localScale = ballStartScale * (1f + 0.16f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        pokemon.SetActive(false);
        ball.localScale = ballStartScale;

        // 3. 球落地 + 小彈跳。
        Vector3 ground = new Vector3(hit.x, 0.42f, hit.z);
        Vector3 startFall = ball.position;
        timer = 0f;
        const float fallTime = 0.60f;
        while (timer < fallTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fallTime);
            float fall = t * t;
            ball.position = Vector3.Lerp(startFall, ground, fall);
            ball.rotation = Quaternion.Slerp(ball.rotation, ballStartRotation, t * 0.18f);
            yield return null;
        }
        ball.position = ground;
        for (int bounce = 0; bounce < 2; bounce++)
        {
            float duration = 0.24f - bounce * 0.07f;
            float height = 0.22f - bounce * 0.12f;
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                ball.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * height);
                yield return null;
            }
        }
        ball.position = ground;

        // 4. 球晃動三次。
        for (int i = 0; i < 3; i++)
        {
            float duration = 0.47f;
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float wobble = Mathf.Sin(t * Mathf.PI * 2f) * (22f - i * 4f);
                ball.rotation = ballStartRotation * Quaternion.Euler(0f, 0f, wobble);
                yield return null;
            }
            ball.rotation = ballStartRotation;
            yield return new WaitForSeconds(0.12f);
        }

        // 5. 成功：彩色星光、擴散光圈。
        Vector3 celebration = ground + Vector3.up * 0.5f;
        CaptureSparkleEffect.Burst(celebration, new Color(0.36f, 1f, 0.87f), 60, 5.2f);
        CaptureSparkleEffect.Burst(celebration, new Color(1f, 0.83f, 0.34f), 32, 3.7f);
        CaptureSparkleEffect.Ring(celebration, new Color(0.39f, 1f, 0.79f), 2.4f);
        CaptureSparkleEffect.Flash(celebration, new Color(0.5f, 1f, 0.8f));
        isCaught = true;
        isBusy = false;
    }

    // Raise ONE straight right arm; while lifting, keep the ball attached to its palm.
    // The right arm uses a shoulder pivot, not a Humanoid bone rig.
    private IEnumerator RaiseStraightArmWithBall(Quaternion target, float duration, Transform ball)
    {
        Quaternion from = throwArmPivot.localRotation;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            throwArmPivot.localRotation = Quaternion.Slerp(from, target, t);
            ball.position = throwPoint.position;
            ball.rotation = ballStartRotation;
            yield return null;
        }
        throwArmPivot.localRotation = target;
        ball.position = throwPoint.position;
        ball.rotation = ballStartRotation;
    }

    private void OnGUI()
    {
        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 29,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            hintStyle = new GUIStyle(statusStyle) { fontSize = 16, fontStyle = FontStyle.Normal };
        }

        float mid = Screen.width * 0.5f;
        GUI.Box(new Rect(mid - 280f, 18f, 560f, 83f), GUIContent.none);
        string title = isCaught ? "CAPTURE SUCCESS!" : isBusy ? "CATCHING..." : "POKEMON CAPTURE DEMO";
        GUI.Label(new Rect(mid - 270f, 25f, 540f, 42f), title, statusStyle);
        GUI.Label(new Rect(mid - 270f, 68f, 540f, 27f),
            isCaught ? "Press R to try again" : isBusy ? "Watch the Poké Ball!" : "SPACE / Left Click: Throw    |    R: Reset", hintStyle);
    }
}
