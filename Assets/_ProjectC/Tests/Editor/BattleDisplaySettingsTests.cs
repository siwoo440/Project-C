using System.Collections.Generic; // 독립 설정 저장소 사용
using NUnit.Framework; // 전투 표시 설정 검사 사용
public sealed class BattleDisplaySettingsTests // 전투 표시 설정 규칙 검사
{ // 표시 설정 검사 시작
    [Test] // 피해 표시 설정 저장 검사 지정
    public void DamageNumbers_RespectSavedPreference() // 피해 숫자 표시 설정 변경과 복원 확인
    { // 피해 숫자 설정 검사 시작
        MemoryStore store = new MemoryStore(); // 독립 설정 저장소 생성
        GraphicsAudioSettings settings = new GraphicsAudioSettings(store, new NoOpApplier()); // 독립 설정 관리자 생성
        Assert.IsTrue(BattleDisplaySettings.ShowDamageNumbers(settings)); // 기본 피해 숫자 표시 확인
        Assert.IsTrue(settings.Change(2, 8, 1)); // 피해 숫자 표시 끄기
        Assert.IsFalse(BattleDisplaySettings.ShowDamageNumbers(settings)); // 피해 숫자 비표시 확인
        GraphicsAudioSettings restored = new GraphicsAudioSettings(store, new NoOpApplier()); // 저장된 설정 복원
        Assert.IsFalse(BattleDisplaySettings.ShowDamageNumbers(restored)); // 피해 숫자 설정 저장 확인
    } // 피해 숫자 설정 검사 종료
    [Test] // 상세 툴팁 표시 방식 검사 지정
    public void Tooltips_RespectHoverAndTemporaryAltKey() // 항상 표시와 임시 키 및 끄기 확인
    { // 툴팁 설정 검사 시작
        MemoryStore store = new MemoryStore(); // 독립 설정 저장소 생성
        GraphicsAudioSettings settings = new GraphicsAudioSettings(store, new NoOpApplier()); // 독립 설정 관리자 생성
        Assert.IsTrue(BattleDisplaySettings.ShowTooltip(settings, true, false)); // 마우스 진입 시 기본 상세 표시 확인
        Assert.IsFalse(BattleDisplaySettings.ShowTooltip(settings, false, true)); // 마우스 이탈 시 상세 숨김 확인
        Assert.IsTrue(settings.Change(2, 9, 1)); // 키를 누를 때만 표시 설정
        Assert.IsFalse(BattleDisplaySettings.ShowTooltip(settings, true, false)); // 키를 누르지 않으면 숨김 확인
        Assert.IsTrue(BattleDisplaySettings.ShowTooltip(settings, true, true)); // 마우스 진입과 키 입력 시 표시 확인
        Assert.IsTrue(settings.Change(2, 9, 1)); // 상세 툴팁 끄기 설정
        Assert.IsFalse(BattleDisplaySettings.ShowTooltip(settings, true, true)); // 키를 눌러도 표시하지 않음 확인
        GraphicsAudioSettings restored = new GraphicsAudioSettings(store, new NoOpApplier()); // 저장된 설정 복원
        Assert.AreEqual("끄기", restored.GetLabel(2, 9)); // 상세 툴팁 설정 저장 확인
    } // 툴팁 설정 검사 종료
    private sealed class MemoryStore : ISettingsStore // 전투 표시 검사 전용 저장소
    { // 독립 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 설정 값 목록
        public int Read(string key, int fallback) // 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 설정 값 기록
        { // 설정 기록 시작
            values[key] = value; // 설정 값 저장
        } // 설정 기록 종료
    } // 독립 저장소 종료
    private sealed class NoOpApplier : ISettingsApplier // 화면 설정 검사 전용 적용기
    { // 임시 적용기 시작
        public void Apply(string key, GraphicsAudioSettings settings) // 설정 적용 요청 수신
        { // 적용 요청 시작
        } // 적용 요청 종료
    } // 임시 적용기 종료
} // 표시 설정 검사 종료
