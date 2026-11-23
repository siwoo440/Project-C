using UnityEngine; // 속도 범위 보정 사용

public static class GameplaySpeedSettings // 전투와 스크롤 속도 변환
{ // 게임 속도 변환 시작
    private static readonly float[] BattleSpeeds = { 1f, 1.5f, 2f }; // 기획 전투 속도 배율
    private static readonly float[] ScrollSpeeds = { 0.65f, 1f, 1.5f }; // 느림과 보통 및 빠름 배율
    public static float BattleMultiplier(GraphicsAudioSettings settings = null) // 현재 전투 속도 배율 조회
    { // 전투 속도 조회 시작
        GraphicsAudioSettings current = settings ?? GraphicsAudioSettingsRuntime.Current; // 현재 또는 테스트 설정 조회
        int index = Mathf.Clamp(current.GetIndex("battleSpeed"), 0, BattleSpeeds.Length - 1); // 전투 속도 번호 보정
        return BattleSpeeds[index]; // 선택한 전투 속도 반환
    } // 전투 속도 조회 종료
    public static float BattleDuration(float duration, GraphicsAudioSettings settings = null) // 전투 대기 시간을 속도에 맞게 변환
    { // 전투 대기 시간 변환 시작
        return Mathf.Max(0f, duration) / BattleMultiplier(settings); // 선택 속도에 맞춘 대기 시간 반환
    } // 전투 대기 시간 변환 종료
    public static float ScrollSensitivity(float baseSensitivity, GraphicsAudioSettings settings = null) // 목록 스크롤 속도 변환
    { // 스크롤 속도 변환 시작
        GraphicsAudioSettings current = settings ?? GraphicsAudioSettingsRuntime.Current; // 현재 또는 테스트 설정 조회
        int index = Mathf.Clamp(current.GetIndex("scrollSpeed"), 0, ScrollSpeeds.Length - 1); // 스크롤 속도 번호 보정
        return Mathf.Max(0f, baseSensitivity) * ScrollSpeeds[index]; // 선택 속도에 맞춘 이동 폭 반환
    } // 스크롤 속도 변환 종료
} // 게임 속도 변환 종료
