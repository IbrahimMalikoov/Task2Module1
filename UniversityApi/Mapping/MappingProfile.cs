using AutoMapper;
using UniversityApi.DTO;
using UniversityApi.Models;

namespace UniversityApi.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Student, StudentDto>().ReverseMap();
        CreateMap<StudentCreateDto, Student>();
        CreateMap<StudentUpdateDto, Student>();

        CreateMap<Teacher, TeacherDto>().ReverseMap();
        CreateMap<TeacherCreateDto, Teacher>();

        CreateMap<Course, CourseDto>().ReverseMap();
        CreateMap<CourseCreateDto, Course>();
        CreateMap<Course, CourseV2Dto>()
            .ForMember(dest => dest.EnrolledStudentsCount, opt => opt.MapFrom(src => src.Enrollments.Count));

        CreateMap<Enrollment, EnrollmentDto>().ReverseMap();
        CreateMap<EnrollmentCreateDto, Enrollment>();
    }
}
