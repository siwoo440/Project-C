using System; // 저장 경로와 형식 조회 사용
using System.Collections; // 플레이 모드 코루틴 사용
using System.IO; // 독립 테스트 저장 파일 사용
using NUnit.Framework; // 편성 저장 검증 사용
using TMPro; // 편성 칸 이름 조회 사용
using UnityEngine; // 화면과 리소스 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 편성 선택 버튼 사용

public sealed class PartyPresetTests // 편성별 저장과 전환 검증
{ // 편성 저장 테스트 시작
    private sealed class UnavailableStatus : IPartyDeploymentStatusProvider // 출전 불가 상태 제공자
    { // 출전 불가 상태 시작
        public bool IsDead(CharacterData character) => true; // 사망 상태 반환
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음 반환
        public bool CanDeploy(CharacterData character) => false; // 출전 불가 상태 반환
    } // 출전 불가 상태 종료
    [Test] // 편집기 테스트 지정
    public void SavedUnavailableMember_RemainsVisibleButCannotDeploy() // 출전 불가 저장 캐릭터 보존 확인
    { // 출전 불가 캐릭터 검증 시작
        CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 캐릭터 원본 조회
        PartySelection selection = new PartySelection(new UnavailableStatus()); // 출전 불가 상태의 편성 생성
        Assert.IsFalse(selection.TryAdd(character)); // 출전 불가 캐릭터 신규 선택 차단 확인
        System.Reflection.MethodInfo restore = typeof(PartySelection).GetMethod("TryRestoreMember", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 저장 편성 복원 기능 조회
        Assert.IsNotNull(restore); // 저장 편성 복원 기능 존재 확인
        Assert.IsTrue((bool)restore.Invoke(selection, new object[] { character })); // 출전 불가 저장 캐릭터 복원 확인
        Assert.AreSame(character, selection.Members[0]); // 저장된 캐릭터 유지 확인
        Assert.IsFalse(selection.Validate().CanDeploy); // 출전 불가 캐릭터 출발 차단 확인
        Assert.IsTrue(selection.TryRemove(character)); // 출전 불가 캐릭터 해제 허용 확인
    } // 출전 불가 캐릭터 검증 종료
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator LegacyAndInvalidPresets_ProtectCurrentState() // 이전 저장과 잘못된 편성 검증
    { // 저장 호환성 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        PartyPresetStore.Reset(); // 독립 편성 상태 초기화
        CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 캐릭터 원본 조회
        Assert.IsTrue(PartyPresetStore.SetMembers(0, new[] { character })); // 유효한 편성 상태 준비
        Assert.IsTrue(LobbySaveService.TryCapture(out string json)); // 현재 저장 내용 생성 확인
        int marker = json.LastIndexOf(",\"presetVersion\":", StringComparison.Ordinal); // 편성 필드 시작 위치 조회
        Assert.Greater(marker, 0); // 저장된 편성 필드 존재 확인
        string legacy = json.Substring(0, marker) + "}"; // 편성 필드가 없는 이전 저장 생성
        Assert.IsTrue(LobbySaveService.CanLoadJson(legacy)); // 이전 저장 형식 유효성 확인
        Assert.IsTrue(LobbySaveService.LoadJson(legacy)); // 이전 저장 형식 복원 확인
        Assert.AreEqual(0, PartyPresetStore.GetMembers(0).Length); // 이전 저장의 빈 편성 초기화 확인
        Assert.IsTrue(PartyPresetStore.SetMembers(0, new[] { character })); // 현재 편성 상태 재설정
        string invalid = json.Substring(0, marker) + json.Substring(marker).Replace("\"ARC_00\"", "\"UNKNOWN_CHARACTER\""); // 존재하지 않는 캐릭터 편성 생성
        Assert.IsFalse(LobbySaveService.CanLoadJson(invalid)); // 잘못된 편성 저장 차단 확인
        Assert.IsFalse(LobbySaveService.LoadJson(invalid)); // 잘못된 편성 복원 차단 확인
        Assert.AreEqual("ARC_00", PartyPresetStore.GetMembers(0)[0]); // 복원 실패 시 기존 편성 유지 확인
        string malformed = json.Substring(0, marker) + ",\"presetVersion\":1,\"presets\":{}}"; // 편성 목록이 누락된 새 저장 생성
        Assert.IsFalse(LobbySaveService.CanLoadJson(malformed)); // 불완전한 새 편성 정보 차단 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 저장 호환성 검증 종료
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator PresetChange_TriggersLobbyAutosave() // 편성 변경 자동 저장 확인
    { // 편성 자동 저장 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 독립 자동 저장 경로 생성
        GameObject host = new GameObject("PresetAutosaveTest"); // 독립 자동 저장 객체 생성
        try // 테스트 상태 정리 보장
        { // 자동 저장 검증 시작
            PartyPresetStore.Reset(); // 독립 편성 상태 초기화
            LobbyAutosaveController autosave = host.AddComponent<LobbyAutosaveController>(); // 자동 저장 관리자 생성
            autosave.Initialize(path); // 독립 자동 저장 경로 연결
            yield return null; // 초기 자동 저장 대기
            yield return null; // 초기 저장 처리 대기
            CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 캐릭터 원본 조회
            Assert.IsTrue(PartyPresetStore.SetMembers(2, new[] { character })); // 세 번째 편성 변경
            yield return null; // 편성 변경 저장 대기
            yield return null; // 자동 저장 처리 대기
            PartyPresetStore.Reset(); // 현재 편성 초기화
            Assert.IsTrue(LobbySaveService.Load(path)); // 자동 저장 파일 복원 확인
            Assert.AreEqual("ARC_00", PartyPresetStore.GetMembers(2)[0]); // 변경한 편성 자동 저장 확인
        } // 자동 저장 검증 종료
        finally // 테스트 상태 정리 시작
        { // 자동 저장과 파일 정리 시작
            UnityEngine.Object.DestroyImmediate(host); // 테스트 자동 저장 관리자 제거
            if (File.Exists(path)) // 테스트 저장 파일 존재 확인
            { // 저장 파일 제거 시작
                File.Delete(path); // 독립 테스트 저장 파일 제거
            } // 저장 파일 제거 종료
        } // 자동 저장과 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 편성 자동 저장 검증 종료
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator Presets_SwitchIndependentlyAndRestoreFromSave() // 편성별 전환과 저장 복원 확인
    { // 편성 저장 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 파일 경로 생성
        GameObject canvasObject = new GameObject("PresetTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        try // 테스트 상태 정리 보장
        { // 편성 전환 검사 시작
            Type storeType = typeof(MainMenuController).Assembly.GetType("PartyPresetStore"); // 편성 저장소 형식 조회
            Assert.IsNotNull(storeType); // 편성 저장소 존재 확인
            storeType.GetMethod("Reset").Invoke(null, null); // 테스트 편성 초기화
            PartyFormationView view = PartyFormationView.Open(canvasObject.GetComponent<Canvas>()); // 편성 화면 열기
            Assert.IsNotNull(view); // 편성 화면 존재 확인
            Transform root = view.transform; // 편성 화면 영역 조회
            Transform grid = root.Find("CharacterList/Viewport/Grid"); // 캐릭터 목록 조회
            Button presetOne = root.Find("Preset_0").GetComponent<Button>(); // 첫 번째 편성 버튼 조회
            Button presetTwo = root.Find("Preset_1").GetComponent<Button>(); // 두 번째 편성 버튼 조회
            Button presetThree = root.Find("Preset_2").GetComponent<Button>(); // 세 번째 편성 버튼 조회
            Button presetFour = root.Find("Preset_3").GetComponent<Button>(); // 네 번째 편성 버튼 조회
            Assert.IsTrue(presetOne.interactable); // 첫 번째 편성 선택 허용 확인
            Assert.IsTrue(presetTwo.interactable); // 두 번째 편성 선택 허용 확인
            Assert.IsTrue(presetThree.interactable); // 세 번째 편성 선택 허용 확인
            Assert.IsTrue(presetFour.interactable); // 네 번째 편성 선택 허용 확인
            grid.Find("Character_0").GetComponent<Button>().onClick.Invoke(); // 첫 편성에 바보 선택
            presetTwo.onClick.Invoke(); // 두 번째 편성 선택
            Assert.AreEqual("빈 슬롯", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 두 번째 편성 독립 상태 확인
            grid.Find("Character_1").GetComponent<Button>().onClick.Invoke(); // 두 번째 편성에 마법사 선택
            presetThree.onClick.Invoke(); // 세 번째 편성 선택
            grid.Find("Character_2").GetComponent<Button>().onClick.Invoke(); // 세 번째 편성에 여사제 선택
            presetFour.onClick.Invoke(); // 네 번째 편성 선택
            grid.Find("Character_3").GetComponent<Button>().onClick.Invoke(); // 네 번째 편성에 여황제 선택
            presetOne.onClick.Invoke(); // 첫 번째 편성으로 전환
            Assert.AreEqual("바보", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 첫 편성 보존 확인
            presetTwo.onClick.Invoke(); // 두 번째 편성으로 전환
            Assert.AreEqual("마법사", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 두 번째 편성 보존 확인
            presetThree.onClick.Invoke(); // 세 번째 편성으로 전환
            Assert.AreEqual("여사제", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 세 번째 편성 보존 확인
            presetFour.onClick.Invoke(); // 네 번째 편성으로 전환
            Assert.AreEqual("여황제", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 네 번째 편성 보존 확인
            Assert.IsTrue(LobbySaveService.Save(path)); // 네 편성의 독립 저장 확인
            storeType.GetMethod("Reset").Invoke(null, null); // 현재 편성 데이터 초기화
            Assert.IsTrue(LobbySaveService.Load(path)); // 저장된 편성 데이터 복원
            UnityEngine.Object.DestroyImmediate(view.gameObject); // 이전 편성 화면 제거
            view = PartyFormationView.Open(canvasObject.GetComponent<Canvas>()); // 복원된 편성 화면 열기
            root = view.transform; // 복원 화면 영역 조회
            Assert.AreEqual("여황제", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 마지막 선택 편성 복원 확인
            root.Find("Preset_2").GetComponent<Button>().onClick.Invoke(); // 복원된 세 번째 편성 선택
            Assert.AreEqual("여사제", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 세 번째 편성 저장 복원 확인
            root.Find("Preset_1").GetComponent<Button>().onClick.Invoke(); // 복원된 두 번째 편성 선택
            Assert.AreEqual("마법사", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 두 번째 편성 저장 복원 확인
            root.Find("Preset_0").GetComponent<Button>().onClick.Invoke(); // 복원된 첫 편성 선택
            Assert.AreEqual("바보", root.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 첫 편성 저장 복원 확인
        } // 편성 전환 검사 종료
        finally // 테스트 상태 정리 시작
        { // 화면과 파일 정리 시작
            UnityEngine.Object.DestroyImmediate(canvasObject); // 테스트 캔버스 제거
            if (File.Exists(path)) // 테스트 저장 파일 존재 확인
            { // 저장 파일 제거 시작
                File.Delete(path); // 독립 테스트 저장 파일 제거
            } // 저장 파일 제거 종료
        } // 화면과 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 편성 저장 검증 종료
} // 편성 저장 테스트 종료
