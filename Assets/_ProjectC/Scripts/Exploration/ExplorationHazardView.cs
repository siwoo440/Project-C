using UnityEngine; // IMGUI 위험 HUD 사용

public sealed class ExplorationHazardView : MonoBehaviour // 퇴색 위험도와 노출도 HUD
{
    private ExplorationHazardRuntime hazardRuntime; // 퇴색 노출 런타임
    private ExplorationSessionManager sessionManager; // 탐사 세션 관리자
    private GUIStyle titleStyle; // 위험 제목 스타일
    private GUIStyle bodyStyle; // 위험 정보 스타일
    private GUIStyle pulseStyle; // 환경 피해 알림 스타일

    public void Configure(
        ExplorationHazardRuntime targetHazardRuntime,
        ExplorationSessionManager targetSessionManager) // 위험 HUD 연결
    {
        hazardRuntime =
            targetHazardRuntime; // 노출 런타임 저장

        sessionManager =
            targetSessionManager; // 탐사 세션 저장
    }

    private void OnGUI() // 현재 위험과 노출도 표시
    {
        if (hazardRuntime == null ||
            sessionManager == null)
        {
            return;
        }

        bool recentPulse =
            Time.unscaledTime -
            hazardRuntime.LastPulseTime <
            1.6f; // 최근 환경 피해 알림 표시 여부

        if (!hazardRuntime.IsInHazard &&
            !recentPulse)
        {
            return; // 안전 지역이며 최근 피해도 없으면 HUD 숨김
        }

        EnsureStyles(); // HUD 스타일 준비

        float width =
            Mathf.Min(
                430f,
                Screen.width - 40f); // 화면 폭에 맞춘 HUD 폭 계산

        Rect panelRect =
            new Rect(
                20f,
                58f,
                width,
                138f); // 화면 왼쪽 위 위험 HUD 영역

        GUI.Box(
            panelRect,
            GUIContent.none); // 위험 HUD 배경 표시

        string title =
            hazardRuntime.IsInHazard
                ? $"퇴색 위험 지역  Lv.{hazardRuntime.CurrentHazardLevel}"
                : "퇴색 피해 발생"; // 현재 지역 제목 구성

        GUI.Label(
            new Rect(
                panelRect.x + 14f,
                panelRect.y + 10f,
                panelRect.width - 28f,
                28f),
            title,
            titleStyle); // 현재 위험 지역 제목 표시

        float exposurePercent =
            hazardRuntime.ExposureNormalized *
            100f; // 노출도 퍼센트 계산

        GUI.Label(
            new Rect(
                panelRect.x + 14f,
                panelRect.y + 42f,
                panelRect.width - 28f,
                24f),
            $"노출도 {exposurePercent:0}% · 피해는 파티 HP/정신력에 즉시 반영",
            bodyStyle); // 노출도와 즉시 피해 반영 안내 표시

        Rect barBackground =
            new Rect(
                panelRect.x + 14f,
                panelRect.y + 73f,
                panelRect.width - 28f,
                18f); // 노출도 바 배경 영역

        GUI.Box(
            barBackground,
            GUIContent.none); // 노출도 바 배경 표시

        Rect barFill =
            new Rect(
                barBackground.x + 2f,
                barBackground.y + 2f,
                (barBackground.width - 4f) *
                hazardRuntime.ExposureNormalized,
                barBackground.height - 4f); // 노출도 채움 영역 계산

        GUI.DrawTexture(
            barFill,
            Texture2D.whiteTexture); // 기본 노출도 채움 표시

        GUI.Label(
            new Rect(
                panelRect.x + 14f,
                panelRect.y + 100f,
                panelRect.width - 28f,
                28f),
            recentPulse
                ? $"퇴색 피해 발생  HP -{hazardRuntime.LastHealthDamage} / 정신 -{hazardRuntime.LastMentalDamage}"
                : "위험 지역을 벗어나면 노출 증가가 멈춥니다.",
            recentPulse
                ? pulseStyle
                : bodyStyle); // 최근 피해 또는 안전 이동 안내 표시
    }

    private void EnsureStyles() // 위험 HUD GUI 스타일 준비
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle =
            new GUIStyle(
                GUI.skin.label); // 제목 스타일 생성

        titleStyle.fontSize = 20; // 제목 글자 크기 설정
        titleStyle.fontStyle = FontStyle.Bold; // 제목 굵게 설정
        titleStyle.normal.textColor = Color.white; // 제목 흰색 설정

        bodyStyle =
            new GUIStyle(
                GUI.skin.label); // 본문 스타일 생성

        bodyStyle.fontSize = 15; // 본문 글자 크기 설정
        bodyStyle.normal.textColor = Color.white; // 본문 흰색 설정

        pulseStyle =
            new GUIStyle(
                bodyStyle); // 피해 알림 스타일 복사

        pulseStyle.fontStyle = FontStyle.Bold; // 피해 알림 굵게 설정
    }
}
