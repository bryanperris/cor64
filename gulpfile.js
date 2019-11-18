const gulp = require('gulp');
const del = require('del');
const util = require('gulp-util');
let {clean, restore, build, test, pack, publish, run} = require('gulp-dotnet-cli');

const buildType = util.env.production ? "Release" : "Debug";

console.log("Build Type: " + buildType);

gulp.task('clean:dotnet', function () {
    return gulp.src("cor64.sln").pipe(clean())
});

gulp.task('clean:binary', function () {
    return del('bin/**/*');
});

gulp.task('build', function() {
    return gulp.src("cor64.sln").pipe(build({
        'configuration': buildType
    }))
});

gulp.task('dist:testroms', function() {
    return gulp.src(['src/RunN64/TestRoms/**/*']).pipe(gulp.dest('bin/TestRoms'));
});

gulp.task('RunN64', ()=>{
    return gulp.src('src/RunN64/RunN64.csproj', {read: false}).pipe(run({
        'additionalArgs': "--noui"
    }));
});

gulp.task('dist', gulp.series('dist:testroms'));

gulp.task('clean', gulp.series('clean:dotnet', 'clean:binary'));

gulp.task('default', gulp.series('clean', 'build', 'dist'));

gulp.task('run', gulp.series('default', 'RunN64'));

gulp.task('fastrun', gulp.series('build', 'RunN64'));