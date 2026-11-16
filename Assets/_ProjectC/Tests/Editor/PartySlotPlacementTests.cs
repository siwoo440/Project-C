using System.Collections; // 실제 저장 코루틴 사용
using NUnit.Framework; // 슬롯 배치 검증 사용
using TMPro; // 캐릭터 상세 문구 확인 사용
using UnityEngine; // 실제 캐릭터 데이터 사용
using UnityEngine.EventSystems; // 슬롯 클릭 이벤트 확인 사용
using UnityEngine.TestTools; // 플레이 모드 저장 검사 사용
using UnityEngine.UI; // 편성 칸 강조 표시 확인 사용
public sealed class PartySlotPlacementTests // 슬롯별 파티 편성 규칙 검사
{ // 슬롯 규칙 검사 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 출전 가능 상태 제공자
    { // 출전 상태 제공 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음 반환
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음 반환
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태 반환
    } // 출전 상태 제공 종료
    [Test] // 빈 칸과 교체 검사 지정
    public void Placement_PreservesGapsAndUsesNumberOrder() // 번호별 위치와 출전 순서 확인
    { // 슬롯 배치 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData first = roster.Characters[0]; // 첫 캐릭터 조회
        CharacterData second = roster.Characters[1]; // 두 번째 캐릭터 조회
        CharacterData third = roster.Characters[2]; // 세 번째 캐릭터 조회
        PartySelection selection = new PartySelection(new AvailableStatus()); // 출전 가능 파티 생성
        Assert.IsTrue(selection.TryPlaceAtSlot(first, 0)); // 왼쪽 네 번째 칸 배치
        Assert.AreSame(first, selection.Slots[0]); // 네 번째 칸 캐릭터 확인
        Assert.IsNull(selection.Slots[1]); // 두 번째 물리 칸의 빈 상태 확인
        Assert.IsNull(selection.Slots[3]); // 첫 번째 번호 칸의 빈 상태 확인
        Assert.IsTrue(selection.TryPlaceAtSlot(second, 3)); // 오른쪽 첫 번째 칸 배치
        Assert.AreSame(second, selection.Members[0]); // 첫 번째 번호의 출전 순서 확인
        Assert.AreSame(first, selection.Members[1]); // 네 번째 번호의 출전 순서 확인
        Assert.IsTrue(selection.TryPlaceAtSlot(first, 3)); // 기존 캐릭터를 사용 중인 칸에 이동
        Assert.AreSame(second, selection.Slots[0]); // 기존 캐릭터 자리 교환 확인
        Assert.AreSame(first, selection.Slots[3]); // 이동한 캐릭터 자리 확인
        Assert.IsTrue(selection.TryPlaceAtSlot(third, 3)); // 새 캐릭터로 사용 중인 칸 교체
        Assert.AreSame(third, selection.Slots[3]); // 새 캐릭터 배치 확인
        CollectionAssert.DoesNotContain(selection.Members, first); // 교체된 캐릭터 제외 확인
        Assert.AreEqual(2, selection.Members.Count); // 교체 시 파티 인원 유지 확인
    } // 슬롯 배치 검사 종료
    [Test] // 저장 형식 호환 검사 지정
    public void Presets_PreserveSparseSlotsAndRestoreLegacyOrder() // 빈 칸 보존과 이전 저장 호환 확인
    { // 편성 저장 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData first = roster.Characters[0]; // 첫 캐릭터 조회
        CharacterData second = roster.Characters[1]; // 두 번째 캐릭터 조회
        PartyPresetStore.Reset(); // 편성 저장소 초기화
        Assert.IsTrue(PartyPresetStore.SetSlots(0, new[] { first, null, null, second })); // 빈 칸 포함 편성 저장
        PartyPresetSnapshot snapshot = PartyPresetStore.Capture(); // 전체 편성 저장 정보 생성
        Assert.IsTrue(PartyPresetStore.IsValid(snapshot, roster)); // 새 저장 정보 유효성 확인
        PartyPresetStore.Reset(); // 현재 편성 초기화
        Assert.IsTrue(PartyPresetStore.Restore(snapshot, roster)); // 새 편성 정보 복원
        string[] slots = PartyPresetStore.GetSlots(0); // 복원된 슬롯 조회
        Assert.AreEqual(first.CharacterId, slots[0]); // 네 번째 번호 슬롯 복원 확인
        Assert.IsTrue(string.IsNullOrEmpty(slots[1])); // 가운데 빈 칸 복원 확인
        Assert.AreEqual(second.CharacterId, slots[3]); // 첫 번째 번호 슬롯 복원 확인
        Assert.AreEqual(second.CharacterId, PartyPresetStore.GetMembers(0)[0]); // 번호 기준 출전 순서 저장 확인
        snapshot.entries[0].slots = null; // 슬롯 정보가 없는 이전 저장 구성
        Assert.IsTrue(PartyPresetStore.Restore(snapshot, roster)); // 이전 저장 복원 확인
        slots = PartyPresetStore.GetSlots(0); // 이전 저장의 슬롯 변환 결과 조회
        Assert.AreEqual(second.CharacterId, slots[3]); // 이전 첫 캐릭터 순서 보존 확인
        Assert.AreEqual(first.CharacterId, slots[2]); // 이전 두 번째 캐릭터 순서 보존 확인
        snapshot.entries[0].slots = new[] { first.CharacterId, first.CharacterId, string.Empty, second.CharacterId }; // 중복 슬롯 저장 구성
        Assert.IsFalse(PartyPresetStore.IsValid(snapshot, roster)); // 중복 슬롯 저장 차단 확인
        PartyPresetStore.Reset(); // 테스트 편성 상태 정리
    } // 편성 저장 검사 종료
    [UnityTest] // 실제 편성 칸 선택 검사 지정
    public IEnumerator OccupiedSlot_ClickShowsCharacterDetailsWithoutChangingParty() // 편성 칸 클릭 상세 정보 확인
    { // 편성 칸 클릭 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        PartyPresetStore.Reset(); // 독립 편성 초기화
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        GameObject canvasObject = new GameObject("FormationCanvas", typeof(RectTransform), typeof(Canvas)); // 편성 화면 캔버스 생성
        PartyFormationView formation = PartyFormationView.Open(canvasObject.GetComponent<Canvas>()); // 실제 편성 화면 열기
        Assert.IsNotNull(formation); // 편성 화면 생성 확인
        formation.DropProfile(roster.Characters[0], 0); // 첫 캐릭터를 왼쪽 칸에 배치
        formation.DropProfile(roster.Characters[1], 3); // 두 번째 캐릭터를 오른쪽 칸에 배치
        Transform slot = formation.transform.Find("Slots/Slot_0"); // 첫 번째 물리 편성 칸 조회
        IPointerClickHandler click = slot.GetComponent<IPointerClickHandler>(); // 실제 슬롯 클릭 처리 조회
        Assert.IsNotNull(click); // 편성 칸 클릭 기능 확인
        click.OnPointerClick(null); // 배치된 캐릭터 칸 클릭
        Assert.AreEqual(roster.Characters[0].DisplayName, formation.transform.Find("DetailPanel/SelectedName").GetComponent<TMP_Text>().text); // 클릭 캐릭터 상세 이름 확인
        StringAssert.Contains(roster.Characters[0].MaxHealth.ToString(), formation.transform.Find("DetailPanel/Stats").GetComponent<TMP_Text>().text); // 클릭 캐릭터 상세 능력치 확인
        Assert.IsTrue(slot.GetComponent<Outline>().enabled); // 클릭한 편성 칸 강조 확인
        Assert.AreEqual(roster.Characters[0].CharacterId, PartyPresetStore.GetSlots(0)[0]); // 클릭 후 기존 편성 유지 확인
        Assert.AreEqual(roster.Characters[1].CharacterId, PartyPresetStore.GetSlots(0)[3]); // 다른 편성 칸 유지 확인
        Transform otherSlot = formation.transform.Find("Slots/Slot_3"); // 다른 편성된 캐릭터 칸 조회
        otherSlot.GetComponent<IPointerClickHandler>().OnPointerClick(null); // 다른 편성 칸 클릭
        Assert.AreEqual(roster.Characters[1].DisplayName, formation.transform.Find("DetailPanel/SelectedName").GetComponent<TMP_Text>().text); // 다른 캐릭터 상세 정보 갱신 확인
        Assert.IsFalse(slot.GetComponent<Outline>().enabled); // 이전 선택 칸 강조 해제 확인
        Assert.IsTrue(otherSlot.GetComponent<Outline>().enabled); // 새 선택 칸 강조 확인
        formation.transform.Find("Slots/Slot_1").GetComponent<IPointerClickHandler>().OnPointerClick(null); // 빈 편성 칸 클릭
        Assert.AreEqual(roster.Characters[1].DisplayName, formation.transform.Find("DetailPanel/SelectedName").GetComponent<TMP_Text>().text); // 빈 칸 클릭 시 기존 상세 정보 유지 확인
        Object.Destroy(canvasObject); // 임시 편성 화면 제거
        PartyPresetStore.Reset(); // 테스트 편성 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 편성 칸 클릭 검사 종료
    [UnityTest] // 실제 저장과 불러오기 검사 지정
    public IEnumerator SparseSlots_RestoreFromActualLobbySave() // 빈 칸 포함 실제 저장 복원 확인
    { // 실제 저장 복원 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData fourth = roster.Characters[0]; // 네 번째 번호 캐릭터 조회
        CharacterData first = roster.Characters[1]; // 첫 번째 번호 캐릭터 조회
        PartyPresetStore.Reset(); // 테스트 편성 초기화
        Assert.IsTrue(PartyPresetStore.SetSlots(0, new[] { fourth, null, null, first })); // 빈 칸 포함 편성 설정
        Assert.IsTrue(LobbySaveService.TryCapture(out string json)); // 실제 로비 저장 정보 생성
        PartyPresetStore.Reset(); // 현재 편성 초기화
        Assert.IsTrue(LobbySaveService.LoadJson(json)); // 실제 로비 저장 복원
        string[] restored = PartyPresetStore.GetSlots(0); // 복원된 슬롯 조회
        Assert.AreEqual(fourth.CharacterId, restored[0]); // 왼쪽 네 번째 번호 복원 확인
        Assert.IsTrue(string.IsNullOrEmpty(restored[1])); // 첫 번째 빈 칸 복원 확인
        Assert.IsTrue(string.IsNullOrEmpty(restored[2])); // 두 번째 빈 칸 복원 확인
        Assert.AreEqual(first.CharacterId, restored[3]); // 오른쪽 첫 번째 번호 복원 확인
        PartyPresetStore.Reset(); // 테스트 편성 상태 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 저장 복원 검사 종료
} // 슬롯 규칙 검사 종료
