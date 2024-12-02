namespace OpenEhr.Library;

public static class Queries
{
    public const string TriageQuery = @"SELECT
                                        c/uid/value as cuid,
                                        c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                        c/content[openEHR-EHR-OBSERVATION.exam.v1,'Ургентность']/data[at0001]/events[at0002]/data[at0003]/items[openEHR-EHR-CLUSTER.sensorum_status_simi.v0,'Ургентность']/items[at0031,'Ургентность']/value/value as urgency,
                                        c/context/start_time/value as start_time,
                                        tags(c) as tags_str
                                        FROM EHR e
                                        CONTAINS COMPOSITION c
                                        WHERE e/ehr_id/value MATCHES {:ehr_id}
                                        AND c/archetype_details/template_id/value='openEHR-EHR-COMPOSITION.t_urgency_triage.v1'
                                        LIMIT 10000";

     public const string AmbulanceQuery = @"SELECT
                                        c/uid/value as cuid,
                                        c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                        c/content[openEHR-EHR-SECTION.adhoc.v1,'Витальные показатели']/items[openEHR-EHR-OBSERVATION.exam.v1,'ОКС, инсульт']/data[at0001]/events[at0002]/data[at0003]/items[openEHR-EHR-CLUSTER.exam_heart.v0,'ОКС']/items[at0003,'ОКС']/value/value as st_erection,
                                        c/context/start_time/value as start_time,
                                        tags(c) as tags_str
                                        FROM EHR e
                                        CONTAINS COMPOSITION c
                                        WHERE e/ehr_id/value MATCHES {:ehr_id}
                                        AND c/archetype_details/template_id/value='openEHR-EHR-COMPOSITION.t_protocol_ambulance.v1'
                                        LIMIT 10000";

    public const string AdmissionQuery = @"SELECT
                                        c/uid/value as cuid,
                                        vo/uid/value as uuid,
                                        c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                        c/content[openEHR-EHR-INSTRUCTION.emergency_hosp.v0]/activities[at0001]/description[at0002]/items[at0003]/items[at0043,'Причина обращения']/value/value as admission_reason,
                                        c/context/start_time/value as start_time
                                        FROM EHR e
                                        CONTAINS COMPOSITION c
                                        CONTAINS VERSIONED_OBJECT vo
                                        WHERE e/ehr_id/value MATCHES {:ehr_id}
                                        AND c/archetype_details/template_id/value='openEHR-EHR-COMPOSITION.t_trearment_in_the_emergency_department.v1'
                                        LIMIT 10000";

    public const string DischargeQuery = @"SELECT
                                              c/uid/value as cuid,
                                              c/context/other_context[at0001]/items/items[at0035]/value/id as ehr_case_id,
                                              c/context/start_time/value as start_time,
                                              c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0092]/value/assigner as emp_id,
                                              tags(c) as tags_str,
                                              c/content[openEHR-EHR-SECTION.adhoc.v1,'Диагноз']/items[openEHR-EHR-SECTION.adhoc.v1,'Диагноз при выписке']/items[openEHR-EHR-SECTION.problems_and_diagnoses.v1,'Диагноз']/items[at0002] as diag_string,
                                              c/content[openEHR-EHR-SECTION.adhoc.v1,'Диагноз']/items[openEHR-EHR-SECTION.adhoc.v1,'Диагноз при выписке']/items[openEHR-EHR-SECTION.problems_and_diagnoses.v1,'Диагноз']/items[at0001]/items[openEHR-EHR-SECTION.adhoc.v1,'Основной диагноз']/items[openEHR-EHR-EVALUATION.problem_diagnosis.v1,'Основной диагноз']/data[at0001]/items[at0002,'Код по МКБ 10']/value/defining_code/code_string as final_icd10_kod
                                              FROM EHR e
                                              CONTAINS COMPOSITION c
                                              CONTAINS VERSIONED_OBJECT vo
                                              WHERE e/ehr_id/value MATCHES {:ehr_id}
                                              AND c/archetype_details/template_id/value = 'openEHR-EHR-COMPOSITION.t_discharge_epicrisis_universal_kis.v2'
                                              AND c tagged by 'sign'
                                              LIMIT 10000";

    public const string MedicalConclusionQuery = @"SELECT
                                              c/uid/value as cuid,
                                              c/context/other_context[at0001]/items/items[at0035]/value/id as ehr_case_id,
                                              c/context/start_time/value as start_time,
                                              c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0092]/value/assigner as emp_id,
                                              tags(c) as tags_str
                                              FROM EHR e
                                              CONTAINS COMPOSITION c
                                              CONTAINS VERSIONED_OBJECT vo
                                              WHERE e/ehr_id/value MATCHES {:ehr_id}
                                              AND c/archetype_details/template_id/value = 'openEHR-EHR-COMPOSITION.t_medical_conclusion.v1'
                                              AND c tagged by 'sign'
                                              LIMIT 10000";

    public const string ExamAdmissionQuery = @"SELECT
                                            c/uid/value as cuid,
                                            c/name/value as name,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                            vo/time_created/value as time_created,
                                            c/context/start_time/value as start_time,
                                            c/archetype_details/template_id/value as template_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0092]/value/assigner as emp_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0038]/value/value as dept,
                                            vo/uid/value as uuid,
                                            tags(c) as tags_str
                                            FROM EHR e
                                            CONTAINS COMPOSITION c
                                            CONTAINS VERSIONED_OBJECT vo
                                            WHERE e/ehr_id/value MATCHES {:ehr_id}
                                            AND c/archetype_details/template_id/value like '*_admis.v0'
                                            AND c tagged by 'sign'
                                            LIMIT 10000";

    public const string ExamnInitialQuery = @"SELECT
                                            c/uid/value as cuid,
                                            c/name/value as name,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                            vo/time_created/value as time_created,
                                            c/context/start_time/value as start_time,
                                            c/archetype_details/template_id/value as template_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0092]/value/assigner as emp_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0038]/value/value as dept,
                                            vo/uid/value as uuid,
                                            tags(c) as tags_str
                                            FROM EHR e
                                            CONTAINS COMPOSITION c
                                            CONTAINS VERSIONED_OBJECT vo
                                            WHERE e/ehr_id/value MATCHES {:ehr_id}
                                            AND c/archetype_details/template_id/value like 'openEHR-EHR-COMPOSITION.t_initial_examination.v0'
                                            AND c tagged by 'sign'
                                            LIMIT 10000";

    public const string ExamResponsibleQuery = @"SELECT
                                            c/uid/value as cuid,
                                            c/name/value as name,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                            vo/time_created/value as time_created,
                                            c/context/start_time/value as start_time,
                                            c/archetype_details/template_id/value as template_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0092]/value/assigner as emp_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0038]/value/value as dept,
                                            vo/uid/value as uuid,
                                            tags(c) as tags_str
                                            FROM EHR e
                                            CONTAINS COMPOSITION c
                                            CONTAINS VERSIONED_OBJECT vo
                                            WHERE e/ehr_id/value MATCHES {:ehr_id}
                                            AND c/archetype_details/template_id/value like '*responsible*'
                                            AND c tagged by 'sign'
                                            LIMIT 10000";

    public const string ExamnQuery = @"SELECT
                                            c/uid/value as cuid,
                                            c/name/value as name,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id,
                                            vo/time_created/value as time_created,
                                            c/context/start_time/value as start_time,
                                            c/archetype_details/template_id/value as template_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0092]/value/assigner as emp_id,
                                            c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0036]/items[at0038]/value/value as dept,
                                            vo/uid/value as uuid,
                                            tags(c) as tags_str
                                            FROM EHR e
                                            CONTAINS COMPOSITION c
                                            CONTAINS VERSIONED_OBJECT vo
                                            WHERE e/ehr_id/value MATCHES {:ehr_id}
                                            AND c/archetype_details/template_id/value like 'openEHR-EHR-COMPOSITION.t_exam_*'
                                            AND c tagged by 'sign'
                                            LIMIT 10000";
}