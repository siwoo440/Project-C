using UnityEngine.InputSystem; // 상세 표시용 키 입력 사용
public static class BattleDisplaySettings // 전투 피드백과 상세 표시 설정
{ // 전투 표시 설정 시작
    public static bool ShowDamageNumbers(GraphicsAudioSettings settings) // 피해 숫자 표시 여부 조회
    { // 피해 숫자 표시 확인 시작
        return settings == null || settings.GetIndex("damageNumbers") != 1; // 끄기 선택을 제외한 피해 숫자 표시
    } // 피해 숫자 표시 확인 종료
    public static bool ShowTooltip(GraphicsAudioSettings settings, bool hovered, bool keyPressed) // 마우스와 설정에 따른 상세 표시 여부 조회
    { // 상세 표시 확인 시작
        if (!hovered) // 현재 대상 마우스 진입 여부 확인
        { // 마우스 이탈 처리 시작
            return false; // 상세 표시 숨김
        } // 마우스 이탈 처리 종료
        int mode = settings == null ? 0 : settings.GetIndex("detailTooltip"); // 선택된 상세 표시 방식 조회
        return mode != 2 && (mode != 1 || keyPressed); // 끄기와 임시 키 입력 조건 반영
    } // 상세 표시 확인 종료
    public static bool IsTooltipKeyPressed() // 임시 상세 표시 키 입력 확인
    { // 키 입력 확인 시작
        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회
        return keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed); // 왼쪽 또는 오른쪽 Alt 입력 확인
    } // 키 입력 확인 종료
} // 전투 표시 설정 종료
