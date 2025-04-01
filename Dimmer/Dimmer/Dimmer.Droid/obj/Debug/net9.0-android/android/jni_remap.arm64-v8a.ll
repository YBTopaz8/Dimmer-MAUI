; ModuleID = 'jni_remap.arm64-v8a.ll'
source_filename = "jni_remap.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.JniRemappingIndexMethodEntry = type {
	%struct.JniRemappingString, ; JniRemappingString name
	%struct.JniRemappingString, ; JniRemappingString signature
	%struct.JniRemappingReplacementMethod ; JniRemappingReplacementMethod replacement
}

%struct.JniRemappingIndexTypeEntry = type {
	%struct.JniRemappingString, ; JniRemappingString name
	i32, ; uint32_t method_count
	ptr ; JniRemappingIndexMethodEntry methods
}

%struct.JniRemappingReplacementMethod = type {
	ptr, ; char* target_type
	ptr, ; char* target_name
	i1 ; bool is_static
}

%struct.JniRemappingString = type {
	i32, ; uint32_t length
	ptr ; char* str
}

%struct.JniRemappingTypeReplacementEntry = type {
	%struct.JniRemappingString, ; JniRemappingString name
	ptr ; char* replacement
}

@jni_remapping_type_replacements = dso_local local_unnamed_addr constant %struct.JniRemappingTypeReplacementEntry zeroinitializer, align 8

@jni_remapping_method_replacement_index = dso_local local_unnamed_addr constant %struct.JniRemappingIndexTypeEntry zeroinitializer, align 8

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/9.0.1xx @ 1719a35b8a0348a4a8dd0061cfc4dd7fe6612a3c"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
