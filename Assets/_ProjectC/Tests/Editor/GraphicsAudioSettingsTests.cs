using System.Collections.Generic; // 독립 설정 저장소 사용
using NUnit.Framework; // 설정 동작 검증 사용
using UnityEngine; // 실제 프레임과 음량 확인

public sealed class GraphicsAudioSettingsTests // 화면과 음량 설정 검증
{ // 설정 기능 테스트 시작
    private sealed class MemoryStore : ISettingsStore // 테스트용 독립 설정 저장소
    { // 독립 저장소 시작
        public readonly Dictionary<string, int> Values = new Dictionary<string, int>(); // 저장된 설정 값 목록
        public int Read(string key, int fallback) // 설정 값 조회
        { // 설정 값 조회 시작
            return Values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 값 조회 종료
        public void Write(string key, int value) // 설정 값 저장
        { // 설정 값 저장 시작
            Values[key] = value; // 지정 설정 값 보관
        } // 설정 값 저장 종료
    } // 독립 저장소 종료
    private sealed class RecordingApplier : ISettingsApplier // 적용 요청 기록기
    { // 적용 요청 기록 시작
        public readonly List<string> Applied = new List<string>(); // 실제 적용 요청 목록
        public void Apply(string key, GraphicsAudioSettings settings) // 설정 적용 요청 기록
        { // 적용 기록 시작
            Applied.Add(key + ":" + settings.GetIndex(key)); // 설정 이름과 선택 값 기록
        } // 적용 기록 종료
    } // 적용 요청 기록 종료
    [Test] // 독립 설정 검사 지정
    public void SupportedSettings_ChangePersistAndRestore() // 지원 설정 변경과 복원 확인
    { // 설정 저장 검증 시작
        MemoryStore store = new MemoryStore(); // 독립 저장소 생성
        RecordingApplier applier = new RecordingApplier(); // 적용 기록기 생성
        GraphicsAudioSettings settings = new GraphicsAudioSettings(store, applier); // 독립 설정 관리자 생성
        Assert.AreEqual("무테창", settings.GetLabel(0, 0)); // 화면 모드 기본값 확인
        Assert.AreEqual("60", settings.GetLabel(0, 2)); // 프레임 기본값 확인
        Assert.AreEqual("80", settings.GetLabel(1, 0)); // 마스터 음량 기본값 확인
        Assert.IsTrue(settings.Change(0, 0, 1)); // 화면 모드 변경 확인
        Assert.AreEqual("창모드", settings.GetLabel(0, 0)); // 화면 모드 순환 확인
        Assert.IsTrue(settings.Change(0, 2, 1)); // 프레임 제한 변경 확인
        Assert.AreEqual("120", settings.GetLabel(0, 2)); // 변경된 프레임 확인
        Assert.IsTrue(settings.Change(1, 0, -1)); // 마스터 음량 변경 확인
        Assert.AreEqual("79", settings.GetLabel(1, 0)); // 변경된 음량 확인
        CollectionAssert.Contains(applier.Applied, "mode:0"); // 화면 모드 적용 요청 확인
        CollectionAssert.Contains(applier.Applied, "frame:2"); // 프레임 적용 요청 확인
        CollectionAssert.Contains(applier.Applied, "master:79"); // 음량 적용 요청 확인
        GraphicsAudioSettings restored = new GraphicsAudioSettings(store, new RecordingApplier()); // 저장 설정 재생성
        Assert.AreEqual("창모드", restored.GetLabel(0, 0)); // 화면 모드 저장 복원 확인
        Assert.AreEqual("120", restored.GetLabel(0, 2)); // 프레임 저장 복원 확인
        Assert.AreEqual("79", restored.GetLabel(1, 0)); // 음량 저장 복원 확인
        Assert.IsFalse(restored.Supports(1, 1)); // 미연결 개별 음량 변경 차단 확인
        Assert.IsFalse(restored.Change(1, 1, 1)); // 미지원 설정 변경 실패 확인
    } // 설정 저장 검증 종료
    [Test] // 손상 설정 검사 지정
    public void InvalidSavedIndexes_UseDefaultsAndApplyInSafeOrder() // 손상 값 복구와 적용 순서 확인
    { // 손상 설정 검사 시작
        MemoryStore store = new MemoryStore(); // 독립 저장소 생성
        store.Values["mode"] = -1; // 잘못된 화면 모드 저장
        store.Values["master"] = 101; // 잘못된 마스터 음량 저장
        RecordingApplier applier = new RecordingApplier(); // 적용 기록기 생성
        GraphicsAudioSettings settings = new GraphicsAudioSettings(store, applier); // 저장 설정 관리자 생성
        Assert.AreEqual("무테창", settings.GetLabel(0, 0)); // 손상 화면 모드 기본 복원 확인
        Assert.AreEqual("80", settings.GetLabel(1, 0)); // 손상 음량 기본 복원 확인
        settings.ApplyAll(); // 저장 설정 전체 적용
        CollectionAssert.AreEqual(new[] { "quality:2", "mode:2", "frame:1", "vsync:1", "master:80" }, applier.Applied); // 품질 다음 동기화 적용 순서 확인
    } // 손상 설정 검사 종료
    [Test] // 음량 범위 검사 지정
    public void MasterVolume_StopsAtMinimumAndMaximum() // 음량 범위 제한 확인
    { // 음량 범위 검사 시작
        MemoryStore store = new MemoryStore(); // 독립 저장소 생성
        store.Values["master"] = 0; // 최소 음량 보관
        RecordingApplier applier = new RecordingApplier(); // 적용 기록기 생성
        GraphicsAudioSettings settings = new GraphicsAudioSettings(store, applier); // 최소 음량 설정 생성
        Assert.IsFalse(settings.Change(1, 0, -1)); // 최소 음량 아래 변경 차단 확인
        Assert.AreEqual("0", settings.GetLabel(1, 0)); // 최소 음량 유지 확인
        store.Values["master"] = 100; // 최대 음량 보관
        settings = new GraphicsAudioSettings(store, applier); // 최대 음량 설정 생성
        Assert.IsFalse(settings.Change(1, 0, 1)); // 최대 음량 위 변경 차단 확인
        Assert.AreEqual("100", settings.GetLabel(1, 0)); // 최대 음량 유지 확인
        Assert.IsEmpty(applier.Applied); // 범위 밖 설정 적용 차단 확인
    } // 음량 범위 검사 종료
    [Test] // Unity 실제 설정 적용 검사 지정
    public void UnityApplier_UpdatesFrameVSyncQualityAndMasterVolume() // 실제 프레임과 음량 적용 확인
    { // 실제 설정 적용 검사 시작
        int originalFrame = Application.targetFrameRate; // 기존 프레임 제한 보관
        int originalVSync = QualitySettings.vSyncCount; // 기존 수직 동기화 보관
        int originalQuality = QualitySettings.GetQualityLevel(); // 기존 품질 단계 보관
        float originalVolume = AudioListener.volume; // 기존 전체 음량 보관
        try // 기존 설정 복구 보장
        { // 실제 설정 적용 검증 시작
            GraphicsAudioSettings settings = new GraphicsAudioSettings(new MemoryStore(), new UnitySettingsApplier()); // 독립 저장소와 Unity 적용기 생성
            Assert.IsTrue(settings.Change(0, 2, 1)); // 프레임 제한 변경
            Assert.AreEqual(120, Application.targetFrameRate); // Unity 프레임 제한 반영 확인
            Assert.IsTrue(settings.Change(0, 3, -1)); // 수직 동기화 켜기
            Assert.AreEqual(1, QualitySettings.vSyncCount); // Unity 수직 동기화 반영 확인
            Assert.IsTrue(settings.Change(0, 7, 1)); // 그래픽 품질 변경
            Assert.AreEqual("Ultra", QualitySettings.names[QualitySettings.GetQualityLevel()]); // Unity 그래픽 품질 반영 확인
            Assert.AreEqual(1, QualitySettings.vSyncCount); // 품질 변경 후 수직 동기화 유지 확인
            Assert.IsTrue(settings.Change(1, 0, -1)); // 전체 음량 변경
            Assert.AreEqual(0.79f, AudioListener.volume, 0.001f); // Unity 전체 음량 반영 확인
        } // 실제 설정 적용 검증 종료
        finally // 기존 Unity 설정 복구
        { // 기존 설정 복구 시작
            QualitySettings.SetQualityLevel(originalQuality, true); // 기존 그래픽 품질 복구
            QualitySettings.vSyncCount = originalVSync; // 기존 수직 동기화 복구
            Application.targetFrameRate = originalFrame; // 기존 프레임 제한 복구
            AudioListener.volume = originalVolume; // 기존 전체 음량 복구
        } // 기존 설정 복구 종료
    } // 실제 설정 적용 검사 종료
} // 설정 기능 테스트 종료
