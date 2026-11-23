using System.Collections.Generic; // 독립 설정 저장소 사용
using NUnit.Framework; // 속도 설정 검증 사용

public sealed class GameplaySpeedSettingsTests // 게임 플레이 속도 설정 검증
{ // 속도 설정 테스트 시작
    private sealed class MemoryStore : ISettingsStore // 독립 속도 설정 저장소
    { // 독립 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 저장된 설정 값 목록
        public int Read(string key, int fallback) // 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 설정 값 저장
        { // 설정 저장 시작
            values[key] = value; // 선택 값 저장
        } // 설정 저장 종료
    } // 독립 저장소 종료
    private sealed class NoOpApplier : ISettingsApplier // 테스트용 적용기
    { // 적용기 시작
        public void Apply(string key, GraphicsAudioSettings settings) // 설정 적용 요청 수신
        { // 설정 적용 요청 시작
        } // 설정 적용 요청 종료
    } // 적용기 종료
    [Test] // 속도 선택 검사 지정
    public void SpeedOptions_ChangePersistAndScaleActualDurations() // 속도 설정 저장과 시간 변환 확인
    { // 속도 설정 검증 시작
        MemoryStore store = new MemoryStore(); // 독립 설정 저장소 생성
        GraphicsAudioSettings settings = new GraphicsAudioSettings(store, new NoOpApplier()); // 독립 설정 관리자 생성
        Assert.AreEqual("x1.5", settings.GetLabel(2, 3)); // 기획 전투 속도 기본값 확인
        Assert.AreEqual(1.5f, GameplaySpeedSettings.BattleMultiplier(settings)); // 기본 전투 속도 배율 확인
        Assert.AreEqual(2f, GameplaySpeedSettings.BattleDuration(3f, settings), 0.001f); // 기본 전투 대기 시간 보정 확인
        Assert.AreEqual(36f, GameplaySpeedSettings.ScrollSensitivity(36f, settings), 0.001f); // 기본 스크롤 속도 확인
        Assert.IsTrue(settings.Change(2, 3, 1)); // 빠른 전투 속도 선택
        Assert.IsTrue(settings.Change(3, 5, 1)); // 빠른 스크롤 속도 선택
        GraphicsAudioSettings restored = new GraphicsAudioSettings(store, new NoOpApplier()); // 저장 설정 복원
        Assert.AreEqual("x2.0", restored.GetLabel(2, 3)); // 저장된 전투 속도 복원 확인
        Assert.AreEqual("빠름", restored.GetLabel(3, 5)); // 저장된 스크롤 속도 복원 확인
        Assert.AreEqual(1.5f, GameplaySpeedSettings.BattleDuration(3f, restored), 0.001f); // 빠른 전투 대기 시간 확인
        Assert.AreEqual(54f, GameplaySpeedSettings.ScrollSensitivity(36f, restored), 0.001f); // 빠른 스크롤 속도 확인
        Assert.IsFalse(restored.Supports(4, 0)); // 미연결 언어 설정 차단 확인
    } // 속도 설정 검증 종료
} // 속도 설정 테스트 종료
