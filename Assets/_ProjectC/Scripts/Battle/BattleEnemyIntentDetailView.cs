using System.Collections.Generic; // 행동 목록 사용
using System.Text; // 상세 설명 문자열 구성 사용
using TMPro; // 상세 설명 텍스트 사용
using UnityEngine; // 런타임 UI 기능 사용
using UnityEngine.UI; // 상세 패널 이미지 사용

public sealed class BattleEnemyIntentDetailView : MonoBehaviour // 적 행동 상세 설명 패널
{
    public static BattleEnemyIntentDetailView Instance
    {
        get;
        private set;
    } // 현재 행동 상세 패널 조회

    private Image backgroundImage; // 상세 패널 배경
    private TMP_Text detailText; // 상세 설명 텍스트
    private BattleEnemyAction currentAction; // 현재 표시 행동
    private BattleUnitView currentActorView; // 현재 표시 적
    private bool currentSpecialPattern; // 현재 특수 패턴 여부
    private bool pinned; // 클릭 고정 여부
    private bool visualCreated; // 상세 패널 화면 생성 여부

    public BattleEnemyAction CurrentAction => currentAction; // 현재 표시 행동 조회
    public bool IsPinned => pinned; // 현재 고정 여부 조회

    public static BattleEnemyIntentDetailView EnsureInstance(
        Canvas canvas) // 상세 설명 패널 존재 보장
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (canvas == null)
        {
            return null;
        }

        GameObject panelObject =
            new GameObject(
                "Day47EnemyIntentDetail",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)); // 상세 설명 패널 오브젝트 생성

        panelObject.transform.SetParent(
            canvas.transform,
            false); // 전투 Canvas에 상세 패널 배치

        panelObject.transform.SetAsLastSibling(); // 상세 패널 최상단 배치

        BattleEnemyIntentDetailView detailView =
            panelObject.AddComponent<BattleEnemyIntentDetailView>(); // 상세 설명 기능 추가

        detailView.EnsureVisual(); // 상세 패널 화면 생성
        detailView.gameObject.SetActive(false); // 시작 시 상세 패널 숨김

        return detailView;
    }

    private void Awake() // 상세 패널 인스턴스 초기화
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject); // 중복 상세 패널 제거
            return;
        }

        Instance = this; // 현재 상세 패널 등록
    }

    public void ShowHover(
        BattleEnemyAction action,
        BattleUnitView actorView,
        bool specialPattern) // 마우스 오버 행동 설명 표시
    {
        if (pinned)
        {
            return;
        }

        ApplyAction(
            action,
            actorView,
            specialPattern); // 마우스 오버 행동 정보 적용

        gameObject.SetActive(
            true); // 행동 설명 표시
    }

    public void HideHover(
        BattleEnemyAction action) // 마우스 이탈 행동 설명 숨김
    {
        if (pinned ||
            currentAction != action)
        {
            return;
        }

        gameObject.SetActive(
            false); // 고정되지 않은 행동 설명 숨김
    }

    public void TogglePinned(
        BattleEnemyAction action,
        BattleUnitView actorView,
        bool specialPattern) // 행동 설명 클릭 고정 전환
    {
        if (pinned &&
            currentAction == action)
        {
            pinned = false; // 같은 행동 재클릭 시 고정 해제
            gameObject.SetActive(false); // 행동 설명 숨김
            return;
        }

        pinned = true; // 행동 설명 고정 활성화

        ApplyAction(
            action,
            actorView,
            specialPattern); // 고정 대상 행동 적용

        gameObject.SetActive(
            true); // 고정 행동 설명 표시
    }

    public void RefreshIfCurrent(
        BattleEnemyAction action,
        BattleUnitView actorView,
        bool specialPattern) // 현재 표시 행동 수치 갱신
    {
        if (currentAction != action)
        {
            return;
        }

        ApplyAction(
            action,
            actorView,
            specialPattern); // 현재 행동 상세 정보 재계산
    }

    public void ValidateActions(
        IReadOnlyList<BattleEnemyAction> actions) // 현재 표시 행동 유효성 확인
    {
        if (currentAction == null)
        {
            return;
        }

        bool stillExists = false; // 현재 행동 존재 여부 초기화

        for (int index = 0;
             index < actions.Count;
             index += 1)
        {
            if (actions[index] == currentAction)
            {
                stillExists = true; // 현재 행동이 예정 목록에 존재
                break;
            }
        }

        if (stillExists)
        {
            return;
        }

        pinned = false; // 제거된 행동의 고정 상태 해제
        currentAction = null; // 현재 행동 제거
        currentActorView = null; // 현재 적 화면 제거
        gameObject.SetActive(false); // 행동 설명 숨김
    }

    public bool IsFocused(
        BattleEnemyAction action) // 행동 설명 선택 여부 확인
    {
        return action != null &&
               currentAction == action &&
               gameObject.activeSelf; // 오버 또는 고정된 행동 여부 반환
    }

    private void ApplyAction(
        BattleEnemyAction action,
        BattleUnitView actorView,
        bool specialPattern) // 행동 상세 데이터 적용
    {
        EnsureVisual(); // 상세 패널 화면 확인

        currentAction = action; // 현재 행동 저장
        currentActorView = actorView; // 현재 적 화면 저장
        currentSpecialPattern = specialPattern; // 특수 패턴 상태 저장

        if (detailText != null)
        {
            detailText.text =
                BuildActionDetailText(); // 행동 정보만 상세 문자열 적용
        }

        transform.SetAsLastSibling(); // 상세 패널 최상단 유지
    }

    private string BuildActionDetailText() // 행동 정보 전용 상세 문자열 생성
    {
        if (currentAction == null ||
            currentAction.Actor == null)
        {
            return "행동 정보 없음";
        }

        StringBuilder builder =
            new StringBuilder(); // 행동 설명 문자열 생성기

        builder.AppendLine(
            pinned
                ? "[행동 고정됨 · 아이콘 다시 클릭 시 해제]"
                : "[행동 아이콘 · 클릭 시 고정]"); // 행동 패널 조작 안내

        if (currentSpecialPattern)
        {
            builder.AppendLine(
                "!!! BOSS SPECIAL PATTERN !!!"); // 보스 특수 패턴 경고
        }

        builder.AppendLine(
            $"행동 : {currentAction.PatternDisplayName}"); // 행동 이름 표시

        builder.AppendLine(
            $"패턴 : {currentAction.PatternIndex} / {currentAction.PatternCount}"); // 패턴 순번 표시

        builder.AppendLine(
            $"행동 순서 : {currentAction.ActionOrder}"); // 행동 순서 표시

        builder.AppendLine(
            $"속도 : {currentAction.ActionSpeed}"); // 행동 속도 표시

        if (currentAction.ActionType == EnemyActionType.Attack)
        {
            int modifiedDamage =
                currentAction.Actor.ModifyOutgoingDamage(
                    currentAction.Amount); // 정신 상태 포함 공격 수치 계산

            BattleDamageResult previewResult =
                currentAction.PreviewDamage(); // 대상 방어 포함 예상 피해 계산

            builder.AppendLine(
                $"유형 : {GetDamageTypeText(currentAction.DamageType)}"); // 피해 유형 표시

            builder.AppendLine(
                $"예상 피해 : {modifiedDamage} → {previewResult.AppliedDamage}"); // 예상 실제 피해 표시
        }
        else if (currentAction.ActionType == EnemyActionType.ApplyStatusEffect)
        {
            string statusName =
                BattleStatusEffectInstance.GetDisplayName(
                    currentAction.StatusEffectType); // 상태 이상 이름 조회

            builder.AppendLine(
                $"효과 : {statusName} {currentAction.Amount}"); // 상태 이상 효과 표시

            builder.AppendLine(
                $"지속 : {currentAction.StatusDuration}T"); // 상태 이상 지속 시간 표시
        }

        builder.AppendLine(
            $"대상 : {(currentAction.Target != null ? currentAction.Target.DisplayName : "없음")}"); // 현재 대상 표시

        if (currentSpecialPattern)
        {
            builder.AppendLine(); // 특수 패턴 경고 전 여백
            builder.AppendLine(
                "경고 : 보스 패턴 순환의 마지막 행동입니다."); // 프로토타입 특수 패턴 규칙 표시
        }

        return builder.ToString(); // 완성 행동 설명 반환
    }

    private static string GetDamageTypeText(
        BattleDamageType damageType) // 피해 유형 표시 이름 조회
    {
        switch (damageType)
        {
            case BattleDamageType.Physical:
                return "물리";

            case BattleDamageType.Magical:
                return "마법";

            default:
                return "일반";
        }
    }

    private void EnsureVisual() // 행동 상세 패널 화면 생성
    {
        if (visualCreated)
        {
            return;
        }

        RectTransform rootRect =
            transform as RectTransform; // 행동 패널 RectTransform 조회

        if (rootRect == null)
        {
            return;
        }

        rootRect.anchorMin =
            new Vector2(
                1f,
                1f); // 오른쪽 위 앵커 설정

        rootRect.anchorMax =
            rootRect.anchorMin; // 앵커 고정

        rootRect.pivot =
            new Vector2(
                1f,
                1f); // 오른쪽 위 피벗 설정

        rootRect.anchoredPosition =
            new Vector2(
                -18f,
                -50f); // 행동 패널 오른쪽 위 위치 설정

        rootRect.sizeDelta =
            new Vector2(
                390f,
                270f); // 행동 정보 전용 패널 크기 설정

        backgroundImage =
            GetComponent<Image>(); // 행동 패널 배경 조회

        if (backgroundImage == null)
        {
            backgroundImage =
                gameObject.AddComponent<Image>(); // 누락 행동 패널 배경 추가
        }

        backgroundImage.color =
            new Color(
                0.035f,
                0.045f,
                0.07f,
                0.96f); // 행동 패널 어두운 배경 적용

        backgroundImage.raycastTarget = false; // 행동 패널 전투 입력 차단 해제

        GameObject textObject =
            new GameObject(
                "DetailText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)); // 행동 설명 텍스트 생성

        textObject.transform.SetParent(
            transform,
            false); // 행동 패널에 텍스트 배치

        detailText =
            textObject.GetComponent<TMP_Text>(); // 행동 설명 TMP 조회

        detailText.fontSize = 18f; // 행동 설명 글자 크기 설정
        detailText.alignment = TextAlignmentOptions.TopLeft; // 왼쪽 위 정렬
        detailText.color = Color.white; // 행동 설명 색상 설정
        detailText.textWrappingMode = TextWrappingModes.Normal; // 최신 TMP 자동 줄바꿈 적용
        detailText.overflowMode = TextOverflowModes.Overflow; // 행동 설명 넘침 허용
        detailText.raycastTarget = false; // 행동 설명 포인터 차단 해제
        detailText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용

        RectTransform textRect =
            detailText.rectTransform; // 행동 설명 RectTransform 조회

        textRect.anchorMin = Vector2.zero; // 최소 앵커 설정
        textRect.anchorMax = Vector2.one; // 최대 앵커 설정
        textRect.offsetMin =
            new Vector2(
                16f,
                14f); // 왼쪽 아래 여백 설정

        textRect.offsetMax =
            new Vector2(
                -16f,
                -14f); // 오른쪽 위 여백 설정

        visualCreated = true; // 행동 패널 화면 생성 완료
    }

    private void OnDestroy() // 행동 패널 제거 처리
    {
        if (Instance == this)
        {
            Instance = null; // 정적 행동 패널 참조 제거
        }
    }
}
